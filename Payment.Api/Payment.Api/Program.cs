using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Payment.Application.Interfaces;
using Payment.Application.Services;
using Payment.Infrastructure.Configuration;
using Payment.Infrastructure.External;
using Payment.Infrastructure.Messaging;
using Payment.Infrastructure.Persistence;
using Payment.Api.Infrastructure;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// ================================
// Serilog Configuration + OpenTelemetry Log Export
// ================================
var otelEndpoint = builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317";

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Payment.Api")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = otelEndpoint;
        options.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
        options.ResourceAttributes = new Dictionary<string, object>
        {
            ["service.name"] = "Payment.Api",
            ["host.name"] = Environment.MachineName,
            ["container.id"] = Environment.MachineName
        };
    })
    .CreateLogger();

builder.Host.UseSerilog();

// ================================
// Configuration Binding
// ================================
builder.Services.Configure<PaytrSettings>(builder.Configuration.GetSection(PaytrSettings.SectionName));
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection(KafkaSettings.SectionName));

// ================================
// Database Configuration
// ================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    Log.Error("Database connection string is not configured! Set ConnectionStrings__DefaultConnection");
    throw new InvalidOperationException("Database connection string 'DefaultConnection' is required.");
}

builder.Services.AddDbContext<PaymentDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    });
});

// ================================
// Ensure Database Tables Exist
// ================================
await EnsureDatabaseTablesExistAsync(connectionString);

// ================================
// Repository & Unit of Work
// ================================
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ================================
// Application Services
// ================================
builder.Services.AddScoped<PaymentService>();

// ================================
// PayTR HTTP Client with Resilience
// ================================
var paytrSettings = builder.Configuration.GetSection(PaytrSettings.SectionName).Get<PaytrSettings>() ?? new PaytrSettings();

builder.Services.AddHttpClient<IPaytrService, PaytrService>(client =>
{
    client.BaseAddress = new Uri(paytrSettings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(paytrSettings.TimeoutSeconds);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

// ================================
// Kafka Producer & Consumers (Optional - only if configured)
// ================================
var kafkaSettings = builder.Configuration.GetSection(KafkaSettings.SectionName).Get<KafkaSettings>() ?? new KafkaSettings();
var kafkaEnabled = !string.IsNullOrEmpty(kafkaSettings.BootstrapServers);

if (kafkaEnabled)
{
    Log.Information("Kafka is enabled. BootstrapServers: {Servers}", kafkaSettings.BootstrapServers);
    builder.Services.AddSingleton<IEventPublisher, KafkaProducer>();
    builder.Services.AddHostedService<OrderCreatedConsumer>();
    builder.Services.AddHostedService<OutboxPublisherWorker>();
}
else
{
    Log.Warning("Kafka is not configured. Messaging features will be disabled.");
    builder.Services.AddSingleton<IEventPublisher, NoOpEventPublisher>();
}

// ================================
// OpenTelemetry Configuration
// ================================
var serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "Payment.Api";
var otlpEndpoint = otelEndpoint;

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName)
        .AddAttributes(new Dictionary<string, object>
        {
            ["environment"] = builder.Environment.EnvironmentName,
            ["host.name"] = Environment.MachineName
        }))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.Filter = httpContext =>
                    !httpContext.Request.Path.StartsWithSegments("/health") &&
                    !httpContext.Request.Path.StartsWithSegments("/swagger");
            })
            .AddHttpClientInstrumentation(options => options.RecordException = true)
            .AddEntityFrameworkCoreInstrumentation(options =>
            {
                options.SetDbStatementForText = true;
                options.SetDbStatementForStoredProcedure = true;
            })
            .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter("Payment.Api")
            .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
    });

// ================================
// Health Checks
// ================================
var healthChecks = builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString, name: "sqlserver", tags: new[] { "db", "ready" });

if (kafkaEnabled)
{
    healthChecks.AddKafka(config =>
    {
        config.BootstrapServers = kafkaSettings.BootstrapServers;
        if (!string.IsNullOrEmpty(kafkaSettings.SaslUsername))
        {
            config.SecurityProtocol = Enum.Parse<Confluent.Kafka.SecurityProtocol>(kafkaSettings.SecurityProtocol, true);
            config.SaslMechanism = Enum.Parse<Confluent.Kafka.SaslMechanism>(kafkaSettings.SaslMechanism, true);
            config.SaslUsername = kafkaSettings.SaslUsername;
            config.SaslPassword = kafkaSettings.SaslPassword;
        }
    }, name: "kafka", tags: new[] { "messaging", "ready" });
}

// ================================
// API Configuration
// ================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Payment API",
        Version = "v1",
        Description = "Payment Service API for e-commerce platform with PayTR integration"
    });
});

var app = builder.Build();

// ================================
// Middleware Pipeline
// ================================
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
    };
});

app.UseSwagger();
app.UseSwaggerUI();

app.UseHealthChecks("/health");
app.UseHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.UseHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // Just checks if app responds
});

app.UseAuthorization();
app.MapControllers();

Log.Information("Payment.Api starting up...");

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

// ================================
// Database Initialization
// ================================
static async Task EnsureDatabaseTablesExistAsync(string connectionString)
{
    try
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        await using var checkCmd = new SqlCommand(
            "SELECT CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'payments') THEN 1 ELSE 0 END", 
            connection);
        var exists = (int)(await checkCmd.ExecuteScalarAsync())! == 1;
        
        if (!exists)
        {
            Log.Information("Creating database tables...");
            
            var createTablesSql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='payments' AND xtype='U')
                CREATE TABLE payments (
                    payment_id UNIQUEIDENTIFIER PRIMARY KEY,
                    order_id UNIQUEIDENTIFIER NOT NULL UNIQUE,
                    user_id UNIQUEIDENTIFIER NOT NULL,
                    amount DECIMAL(18, 2) NOT NULL,
                    currency NVARCHAR(10) NOT NULL,
                    status INT NOT NULL,
                    provider_reference NVARCHAR(500),
                    failure_reason NVARCHAR(1000),
                    created_at DATETIME2 NOT NULL,
                    updated_at DATETIME2 NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='payment_attempts' AND xtype='U')
                CREATE TABLE payment_attempts (
                    attempt_id UNIQUEIDENTIFIER PRIMARY KEY,
                    payment_id UNIQUEIDENTIFIER NOT NULL REFERENCES payments(payment_id) ON DELETE CASCADE,
                    provider NVARCHAR(50) NOT NULL,
                    provider_reference NVARCHAR(500),
                    status INT NOT NULL,
                    request_payload NVARCHAR(MAX),
                    response_payload NVARCHAR(MAX),
                    error_message NVARCHAR(1000),
                    created_at DATETIME2 NOT NULL,
                    completed_at DATETIME2
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='outbox_messages' AND xtype='U')
                CREATE TABLE outbox_messages (
                    message_id UNIQUEIDENTIFIER PRIMARY KEY,
                    type NVARCHAR(200) NOT NULL,
                    payload NVARCHAR(MAX) NOT NULL,
                    correlation_id NVARCHAR(100),
                    occurred_at DATETIME2 NOT NULL,
                    processed_at DATETIME2,
                    retry_count INT NOT NULL DEFAULT 0,
                    last_error NVARCHAR(2000)
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='refunds' AND xtype='U')
                CREATE TABLE refunds (
                    refund_id UNIQUEIDENTIFIER PRIMARY KEY,
                    payment_id UNIQUEIDENTIFIER NOT NULL REFERENCES payments(payment_id) ON DELETE CASCADE,
                    amount DECIMAL(18, 2) NOT NULL,
                    currency NVARCHAR(10) NOT NULL,
                    status INT NOT NULL,
                    reason NVARCHAR(500),
                    provider_reference NVARCHAR(500),
                    failure_reason NVARCHAR(1000),
                    created_at DATETIME2 NOT NULL,
                    completed_at DATETIME2
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='ix_payments_order_id')
                CREATE INDEX ix_payments_order_id ON payments(order_id);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='ix_payment_attempts_provider_reference')
                CREATE INDEX ix_payment_attempts_provider_reference ON payment_attempts(provider_reference);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='ix_payment_attempts_payment_id')
                CREATE INDEX ix_payment_attempts_payment_id ON payment_attempts(payment_id);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='ix_outbox_pending')
                CREATE INDEX ix_outbox_pending ON outbox_messages(processed_at, occurred_at);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='ix_refunds_payment_id')
                CREATE INDEX ix_refunds_payment_id ON refunds(payment_id);
            ";
            
            await using var createCmd = new SqlCommand(createTablesSql, connection);
            await createCmd.ExecuteNonQueryAsync();
            
            Log.Information("Database tables created successfully");
        }
        else
        {
            Log.Information("Database tables already exist");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to ensure database tables exist");
        throw;
    }
}

// ================================
// Polly Policies
// ================================
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}
