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
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// ================================
// Serilog Configuration
// ================================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Payment.Api")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
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

// Support Railway's DATABASE_URL format
if (string.IsNullOrEmpty(connectionString))
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrEmpty(databaseUrl))
    {
        connectionString = ConvertDatabaseUrlToConnectionString(databaseUrl);
        Log.Information("Using DATABASE_URL environment variable");
    }
}

if (string.IsNullOrEmpty(connectionString))
{
    Log.Error("Database connection string is not configured! Set ConnectionStrings__DefaultConnection or DATABASE_URL");
    throw new InvalidOperationException("Database connection string 'DefaultConnection' or DATABASE_URL environment variable is required.");
}

builder.Services.AddDbContext<PaymentDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
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
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        if (!string.IsNullOrEmpty(otlpEndpoint))
        {
            tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        if (!string.IsNullOrEmpty(otlpEndpoint))
        {
            metrics.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
        }
    });

// ================================
// Health Checks
// ================================
var healthChecks = builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: new[] { "db", "ready" });

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

// Enable Swagger in all environments for Railway
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
// Helper: Convert DATABASE_URL to Npgsql Connection String
// ================================
static string ConvertDatabaseUrlToConnectionString(string databaseUrl)
{
    // Format: postgresql://user:password@host:port/database
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    var username = userInfo[0];
    var password = userInfo.Length > 1 ? userInfo[1] : "";
    var host = uri.Host;
    var port = uri.Port > 0 ? uri.Port : 5432;
    var database = uri.AbsolutePath.TrimStart('/');
    
    return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
}

// ================================
// Database Initialization
// ================================
static async Task EnsureDatabaseTablesExistAsync(string connectionString)
{
    try
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        await using var checkCmd = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'payments')", 
            connection);
        var exists = (bool)(await checkCmd.ExecuteScalarAsync())!;
        
        if (!exists)
        {
            Log.Information("Creating database tables...");
            
            var createTablesSql = @"
                CREATE TABLE IF NOT EXISTS payments (
                    payment_id UUID PRIMARY KEY,
                    order_id UUID NOT NULL UNIQUE,
                    user_id UUID NOT NULL,
                    amount DECIMAL(18, 2) NOT NULL,
                    currency VARCHAR(10) NOT NULL,
                    status INTEGER NOT NULL,
                    provider_reference VARCHAR(500),
                    failure_reason VARCHAR(1000),
                    created_at TIMESTAMP NOT NULL,
                    updated_at TIMESTAMP NOT NULL
                );

                CREATE TABLE IF NOT EXISTS payment_attempts (
                    attempt_id UUID PRIMARY KEY,
                    payment_id UUID NOT NULL REFERENCES payments(payment_id) ON DELETE CASCADE,
                    provider VARCHAR(50) NOT NULL,
                    provider_reference VARCHAR(500),
                    status INTEGER NOT NULL,
                    request_payload TEXT,
                    response_payload TEXT,
                    error_message VARCHAR(1000),
                    created_at TIMESTAMP NOT NULL,
                    completed_at TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS outbox_messages (
                    message_id UUID PRIMARY KEY,
                    type VARCHAR(200) NOT NULL,
                    payload TEXT NOT NULL,
                    correlation_id VARCHAR(100),
                    occurred_at TIMESTAMP NOT NULL,
                    processed_at TIMESTAMP,
                    retry_count INTEGER NOT NULL DEFAULT 0,
                    last_error VARCHAR(2000)
                );

                CREATE TABLE IF NOT EXISTS refunds (
                    refund_id UUID PRIMARY KEY,
                    payment_id UUID NOT NULL REFERENCES payments(payment_id) ON DELETE CASCADE,
                    amount DECIMAL(18, 2) NOT NULL,
                    currency VARCHAR(10) NOT NULL,
                    status INTEGER NOT NULL,
                    reason VARCHAR(500),
                    provider_reference VARCHAR(500),
                    failure_reason VARCHAR(1000),
                    created_at TIMESTAMP NOT NULL,
                    completed_at TIMESTAMP
                );

                CREATE INDEX IF NOT EXISTS ix_payments_order_id ON payments(order_id);
                CREATE INDEX IF NOT EXISTS ix_payment_attempts_provider_reference ON payment_attempts(provider_reference);
                CREATE INDEX IF NOT EXISTS ix_payment_attempts_payment_id ON payment_attempts(payment_id);
                CREATE INDEX IF NOT EXISTS ix_outbox_pending ON outbox_messages(processed_at, occurred_at);
                CREATE INDEX IF NOT EXISTS ix_refunds_payment_id ON refunds(payment_id);
            ";
            
            await using var createCmd = new NpgsqlCommand(createTablesSql, connection);
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
