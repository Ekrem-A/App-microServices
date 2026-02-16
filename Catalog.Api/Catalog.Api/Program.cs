using Catalog.Api.Middleware;
using Catalog.Application;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Data;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ═══════════════════════════════════════════════════════════
    // SERILOG + OPENTELEMETRY LOG EXPORT
    // ═══════════════════════════════════════════════════════════
    
    var otlpEndpoint = builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317";

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Catalog.Api")
        .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
        .WriteTo.Console()
        .WriteTo.File(
            path: "Logs/catalog-api-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
            shared: true)
        .WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = otlpEndpoint;
            options.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
            options.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = "Catalog.Api",
                ["host.name"] = Environment.MachineName,
                ["container.id"] = Environment.MachineName
            };
        })
        .CreateLogger();

    // Use Serilog instead of default logger
    builder.Host.UseSerilog();

    Log.Information("🚀 Starting Catalog API...");

// ═══════════════════════════════════════════════════════════
// OPENTELEMETRY CONFIGURATION
// ═══════════════════════════════════════════════════════════

var serviceName = "Catalog.Api";
var serviceVersion = "1.0.0";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
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
        .AddHttpClientInstrumentation(options =>
        {
            options.RecordException = true;
        })
        .AddEntityFrameworkCoreInstrumentation(options =>
        {
            options.SetDbStatementForText = true;
            options.SetDbStatementForStoredProcedure = true;
        })
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(otlpEndpoint);
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("Catalog.Api")
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(otlpEndpoint);
        }));

Log.Information("📊 OpenTelemetry configured with endpoint: {Endpoint}", otlpEndpoint);

// ═══════════════════════════════════════════════════════════
// SECURITY CONFIGURATION
// ═══════════════════════════════════════════════════════════

// API Key Settings
builder.Services.Configure<ApiKeySettings>(
    builder.Configuration.GetSection("ApiKeySettings"));

// Rate Limiting Settings
builder.Services.Configure<RateLimitSettings>(
    builder.Configuration.GetSection("RateLimitSettings"));

// Memory Cache for Rate Limiting
builder.Services.AddMemoryCache();

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() 
                           ?? Array.Empty<string>();
        
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            // Development: Allow all (NOT for production!)
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

// Request Size Limit (10MB default)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB
});

// ═══════════════════════════════════════════════════════════
// APPLICATION SERVICES
// ═══════════════════════════════════════════════════════════

// Add Application Layer (MediatR, AutoMapper, FluentValidation, Behaviors)
builder.Services.AddApplication();

// Add Infrastructure Layer (DbContext, UnitOfWork, Redis, Kafka)
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<CatalogDbContext>("database");

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Catalog API", 
        Version = "v1",
        Description = "Catalog microservice API for e-commerce platform"
    });
    
    // Add API Key security definition to Swagger
    c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "API Key needed to access the endpoints. X-API-Key: YOUR_API_KEY",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-API-Key",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Auto migrate database on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("✅ Database migrations applied successfully");
        
        // Seed brands if table is empty
        var brandCount = await dbContext.Brands.CountAsync();
        var brands = new List<Brand>();
        if (brandCount == 0)
        {
            logger.LogInformation("📦 Seeding brands...");
            brands = Catalog.Infrastructure.Data.Seeds.BrandSeedData.GetBrands();
            
            if (brands != null && brands.Any())
            {
                await dbContext.Brands.AddRangeAsync(brands);
                var savedCount = await dbContext.SaveChangesAsync();
                logger.LogInformation("✅ Successfully seeded {SavedCount} brands (Total: {TotalCount})", savedCount, brands.Count);
            }
            else
            {
                logger.LogWarning("⚠️ No brands found in seed data");
            }
        }
        else
        {
            logger.LogInformation("ℹ️ Brands already exist ({Count} brands), skipping seed", brandCount);
            brands = await dbContext.Brands.ToListAsync();
        }
        
        // Seed categories if table is empty
        var categoryCount = await dbContext.Categories.CountAsync();
        var categories = new List<Category>();
        if (categoryCount == 0)
        {
            logger.LogInformation("📦 Seeding categories...");
            categories = Catalog.Infrastructure.Data.Seeds.CategorySeedData.GetCategories();
            
            if (categories != null && categories.Any())
            {
                await dbContext.Categories.AddRangeAsync(categories);
                var savedCount = await dbContext.SaveChangesAsync();
                logger.LogInformation("✅ Successfully seeded {SavedCount} categories (Total: {TotalCount})", savedCount, categories.Count);
            }
            else
            {
                logger.LogWarning("⚠️ No categories found in seed data");
            }
        }
        else
        {
            logger.LogInformation("ℹ️ Categories already exist ({Count} categories), skipping seed", categoryCount);
            categories = await dbContext.Categories.ToListAsync();
        }
        
        // Seed products if table is empty (requires categories and brands)
        var productCount = await dbContext.Products.CountAsync();
        if (productCount == 0 && categories.Any() && brands.Any())
        {
            logger.LogInformation("📦 Seeding products...");
            var products = Catalog.Infrastructure.Data.Seeds.ProductSeedData.GetProducts(categories, brands);
            
            if (products != null && products.Any())
            {
                await dbContext.Products.AddRangeAsync(products);
                var savedCount = await dbContext.SaveChangesAsync();
                logger.LogInformation("✅ Successfully seeded {SavedCount} products (Total: {TotalCount})", savedCount, products.Count);
            }
            else
            {
                logger.LogWarning("⚠️ No products found in seed data");
            }
        }
        else if (productCount > 0)
        {
            logger.LogInformation("ℹ️ Products already exist ({Count} products), skipping seed", productCount);
        }
        else if (!categories.Any() || !brands.Any())
        {
            logger.LogWarning("⚠️ Cannot seed products: Categories or Brands are missing");
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// ═══════════════════════════════════════════════════════════
// MIDDLEWARE PIPELINE
// ═══════════════════════════════════════════════════════════

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

// Health check endpoint (before authentication)
app.MapHealthChecks("/health");

// Security headers (first middleware)
app.UseMiddleware<SecurityHeadersMiddleware>();

// HTTPS Redirection
app.UseHttpsRedirection();

// CORS
app.UseCors("AllowedOrigins");

// Rate Limiting (before authentication to prevent brute force)
app.UseMiddleware<RateLimitingMiddleware>();

// API Key Authentication
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

// Authorization
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Check if this is being run as part of EF Core migration command
// EF Core sets DOTNET_ENVIRONMENT to "Development" and uses specific patterns
var isMigrationCommand = Environment.GetCommandLineArgs().Any(arg => 
    arg.Contains("ef", StringComparison.OrdinalIgnoreCase) || 
    arg.Contains("migration", StringComparison.OrdinalIgnoreCase));

if (!isMigrationCommand)
{
    Log.Information("🔐 Catalog API security enabled:");
    Log.Information("  ✓ API Key Authentication");
    Log.Information("  ✓ Rate Limiting");
    Log.Information("  ✓ Security Headers");
    Log.Information("  ✓ CORS Policy");
    Log.Information("  ✓ HTTPS Enforcement");
    Log.Information("  ✓ File Logging Enabled");

    app.Run();
}
else
{
    // If running as part of migration command, don't start the web server
    // Migration will complete and host will be disposed automatically
    Log.Information("ℹ️ Running as part of EF Core migration command, skipping web server startup");
}
}
catch (Microsoft.Extensions.Hosting.HostAbortedException)
{
    // HostAbortedException is normal when running EF Core migration commands
    // (dotnet ef database update, etc.). Ignore it silently.
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}



