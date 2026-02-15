using Cart.Api.Middleware;
using Cart.Application;
using Cart.Infrastructure;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Startup logging
Console.WriteLine("===========================================");
Console.WriteLine("Cart.Api Starting...");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"REDIS_URL exists: {!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("REDIS_URL"))}");
Console.WriteLine("===========================================");

// Railway PORT support
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
Console.WriteLine($"Configuring to listen on port: {port}");
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ================================
// OpenTelemetry
// ================================
var otelEndpoint = builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "Cart.Api")
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
        .AddRedisInstrumentation()
        .AddOtlpExporter(options => options.Endpoint = new Uri(otelEndpoint)))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("Cart.Api")
        .AddOtlpExporter(options => options.Endpoint = new Uri(otelEndpoint)));

Console.WriteLine($"OpenTelemetry configured with endpoint: {otelEndpoint}");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Cart API",
        Version = "v1",
        Description = "Shopping Cart microservice API with CQRS pattern"
    });
});

// Add Application layer (MediatR, Validators)
builder.Services.AddApplication();

// Add Infrastructure layer (Redis, Kafka)
builder.Services.AddInfrastructure(builder.Configuration);

// Health checks - Get Redis connection string (supports Railway REDIS_URL)
var redisConnectionString = GetRedisConnectionString(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddRedis(redisConnectionString, name: "redis", tags: new[] { "ready" }, timeout: TimeSpan.FromSeconds(5));

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseExceptionHandling();

// Enable Swagger in all environments for Railway
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Cart API v1");
    options.RoutePrefix = "swagger"; // Access at /swagger
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Health check endpoints
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

Console.WriteLine("===========================================");
Console.WriteLine($"Cart.Api is starting on port {port}");
Console.WriteLine("Health endpoints: /health/live, /health/ready");
Console.WriteLine("Swagger: /swagger");
Console.WriteLine("===========================================");

app.Run();

// Helper method to parse Redis connection string from Railway REDIS_URL format
static string GetRedisConnectionString(IConfiguration configuration)
{
    // Check for Railway's REDIS_URL environment variable first
    var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL");
    
    if (!string.IsNullOrEmpty(redisUrl))
    {
        // Railway format: redis://default:password@host:port
        // StackExchange.Redis format: host:port,password=password,ssl=false
        try
        {
            var uri = new Uri(redisUrl);
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 6379;
            var password = uri.UserInfo.Contains(':') 
                ? uri.UserInfo.Split(':')[1] 
                : uri.UserInfo;
            
            if (!string.IsNullOrEmpty(password))
            {
                return $"{host}:{port},password={password},abortConnect=false";
            }
            return $"{host}:{port},abortConnect=false";
        }
        catch
        {
            // If parsing fails, try using it as-is
            return redisUrl;
        }
    }
    
    // Fallback to configuration
    return configuration["Redis:ConnectionString"] ?? "localhost:6379";
}
