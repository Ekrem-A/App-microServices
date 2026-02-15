using AspNetCoreRateLimit;
using Idendity.Api.Middleware;
using Idendity.Application;
using Idendity.Infrastructure;
using Idendity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

var builder = WebApplication.CreateBuilder(args);

// Railway sets PORT for inbound traffic. If present, listen on that port.
var appPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(appPort))
{
    builder.WebHost.UseUrls($"http://+:{appPort}");
}

// If ConnectionStrings:DefaultConnection is not set, throw an error.
if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
{
    throw new InvalidOperationException(
        "Database connection is not configured. Set ConnectionStrings:DefaultConnection.");
}

// Configure Serilog with Elasticsearch
var elasticsearchUri = builder.Configuration["ElasticSearch:Uri"] ?? "http://localhost:9200";

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Application", "IdentityService")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticsearchUri))
    {
        AutoRegisterTemplate = true,
        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
        IndexFormat = $"identity-api-logs-{builder.Environment.EnvironmentName.ToLower()}-{DateTime.UtcNow:yyyy-MM}",
        NumberOfReplicas = 0,
        NumberOfShards = 1,
        EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog | EmitEventFailureHandling.RaiseCallback,
        FailureCallback = (logEvent, exception) => Console.WriteLine($"Unable to submit event to Elasticsearch: {exception?.Message}"),
        ModifyConnectionSettings = conn =>
        {
            conn.ServerCertificateValidationCallback((sender, certificate, chain, errors) => true);
            return conn;
        }
    })
    .CreateLogger();

builder.Host.UseSerilog();
    
// Add OpenTelemetry with service defaults
builder.AddServiceDefaults();

// ================================
// OpenTelemetry
// ================================
var otelEndpoint = builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "Identity.Api")
        .AddAttributes(new Dictionary<string, object>
        {
            ["environment"] = builder.Environment.EnvironmentName,
            ["host.name"] = Environment.MachineName
        }))
    .WithTracing(tracing => tracing
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
        .AddOtlpExporter(options => options.Endpoint = new Uri(otelEndpoint)))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("Identity.Api")
        .AddOtlpExporter(options => options.Endpoint = new Uri(otelEndpoint)));

Log.Information("OpenTelemetry configured with endpoint: {Endpoint}", otelEndpoint);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with JWT authentication
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Identity API",
        Version = "v1",
        Description = "E-Commerce Identity Service API - Authentication and Authorization",
        Contact = new OpenApiContact
        {
            Name = "API Support"
        }
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token.\n\nExample: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add Application layer services
builder.Services.AddApplication();

// Add Infrastructure layer services (Identity, EF Core, JWT)
builder.Services.AddInfrastructure(builder.Configuration);

// Configure Rate Limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.RealIpHeader = "X-Forwarded-For";
    options.ClientIdHeader = "X-ClientId";
    options.GeneralRules = new List<RateLimitRule>
    {
        new()
        {
            Endpoint = "POST:/api/auth/login",
            Period = "1m",
            Limit = 5 // 5 login attempts per minute
        },
        new()
        {
            Endpoint = "POST:/api/auth/register",
            Period = "1h",
            Limit = 10 // 10 registrations per hour per IP
        },
        new()
        {
            Endpoint = "*",
            Period = "1s",
            Limit = 10 // General rate limit
        }
    };
});
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000", "https://localhost:3000" };
        
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add Dapr client (for service-to-service communication)
builder.Services.AddDaprClient();

var app = builder.Build();

// Global exception handling (should be first)
app.UseExceptionHandling();

// Add Serilog request logging
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
        diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());
        
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value);
        }
    };
});

// Audit logging
app.UseAuditLogging();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity API v1");
        c.RoutePrefix = "swagger";
    });
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'";
    context.Response.Headers["Permissions-Policy"] = "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";
    await next();
});

app.UseHttpsRedirection();

app.UseCors("AllowSpecificOrigins");

app.UseIpRateLimiting();

app.UseAuthentication();
app.UseAuthorization();

// Cloud events for Dapr pub/sub
app.UseCloudEvents();

app.MapControllers();
app.MapSubscribeHandler(); // Dapr pub/sub endpoint

// Map health check endpoints
app.MapDefaultEndpoints();

// Optional: run EF Core migrations automatically on startup (useful for Railway)
// Set RUN_MIGRATIONS=true in environment to enable.
if (string.Equals(Environment.GetEnvironmentVariable("RUN_MIGRATIONS"), "true", StringComparison.OrdinalIgnoreCase))
{
    using var migrationScope = app.Services.CreateScope();
    var db = migrationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    Log.Information("RUN_MIGRATIONS=true detected. Applying EF Core migrations...");
    await db.Database.MigrateAsync();
    Log.Information("EF Core migrations applied successfully.");
}

// Seed roles on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        await Idendity.Infrastructure.DependencyInjection.SeedRolesAsync(scope.ServiceProvider);
        Log.Information("Roles seeded successfully");
        
        await Idendity.Infrastructure.DependencyInjection.SeedAdminUserAsync(scope.ServiceProvider);
        Log.Information("Admin user seeded successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while seeding roles or admin user");
    }
}

Log.Information("Identity API starting up on {Environment} environment", app.Environment.EnvironmentName);

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
