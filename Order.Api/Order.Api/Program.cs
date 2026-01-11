using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Order.Api.Extensions;
using Order.Api.Middleware;
using Order.Api.Services;
using Order.Application;
using Order.Application.Common.Interfaces;
using Order.Infrastructure;
using Order.Infrastructure.Persistence;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Railway configuration (DATABASE_URL, PORT)
builder.ConfigureForRailway();

// Configure Serilog - replace default logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Order.Api")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog(Log.Logger, dispose: true);
builder.Logging.ClearProviders(); // Remove default console logger to prevent duplicates

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// DataProtection - Use ephemeral provider for containerized environments
// Since we use JWT (not cookies), DataProtection keys don't need to persist
builder.Services.AddDataProtection()
    .UseEphemeralDataProtectionProvider();

// Add Application and Infrastructure layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// JWT Authentication
var authConfig = builder.Configuration.GetSection("Authentication");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var authority = authConfig["Authority"];
    var audience = authConfig["Audience"];

    if (!string.IsNullOrWhiteSpace(authority))
    {
        options.Authority = authority;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    }

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = !string.IsNullOrWhiteSpace(authority),
        ValidIssuer = authority,
        ValidateAudience = !string.IsNullOrWhiteSpace(audience),
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(5),
        ValidateIssuerSigningKey = true
    };

    // For development without Identity service - use symmetric key
    var secretKey = authConfig["SecretKey"];
    if (!string.IsNullOrWhiteSpace(secretKey))
    {
        options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey));
    }

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Log.Warning("Authentication failed: {Error}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var userId = context.Principal?.FindFirst("sub")?.Value
                ?? context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            Log.Information("Token validated for user: {UserId}", userId);
            return Task.CompletedTask;
        }
    };
});

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Orders.Read", policy =>
    {
        policy.RequireAuthenticatedUser();
        // Check for scope or role claim
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "scope" && c.Value.Contains("orders.read")) ||
            context.User.HasClaim(c => c.Type == "role" && (c.Value == "Admin" || c.Value == "User")) ||
            context.User.Identity?.IsAuthenticated == true);
    });

    options.AddPolicy("Orders.Write", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "scope" && c.Value.Contains("orders.write")) ||
            context.User.HasClaim(c => c.Type == "role" && (c.Value == "Admin" || c.Value == "User")) ||
            context.User.Identity?.IsAuthenticated == true);
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowConfiguredOrigins", corsBuilder =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000", "https://localhost:3000" };

        corsBuilder
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// Rate Limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

builder.Services.AddControllers();

// Health Checks
builder.Services.AddCustomHealthChecks(builder.Configuration);

// Swagger with JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Order API",
        Version = "v1",
        Description = "E-Commerce Order Microservice API",
        Contact = new OpenApiContact
        {
            Name = "Order Service Team"
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Apply migrations at startup when configured
var autoMigrate = builder.Configuration.GetValue<bool>("Database:AutoMigrate");
if (autoMigrate)
{
    using var scope = app.Services.CreateScope();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        Log.Information("Applying database migrations...");
        db.Database.Migrate();
        Log.Information("Database migrations applied.");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Failed to apply database migrations");
        throw;
    }
}

//// Initialize database with initializer if you have one (kept for reference)
//using (var scope = app.Services.CreateScope())
//{
//    var initializer = scope.ServiceProvider.GetRequiredService<Order.Infrastructure.Persistence.OrderDbContextInitializer>();
//    await initializer.InitializeAsync();
//    await initializer.SeedAsync();
//}

// Configure the HTTP request pipeline.

// Correlation ID (first in pipeline)
app.UseMiddleware<CorrelationIdMiddleware>();

// Exception handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Rate limiting
app.UseIpRateLimiting();

// Swagger - only in development or when explicitly enabled
var enableSwagger = builder.Configuration.GetValue<bool>("EnableSwagger");
if (app.Environment.IsDevelopment() || enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order API v1");
        c.RoutePrefix = "swagger";
    });
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseHttpsRedirection();

// HSTS
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseCors("AllowConfiguredOrigins");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health checks
app.MapCustomHealthChecks();

// Request logging
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.FirstOrDefault());

        if (httpContext.Items.TryGetValue("CorrelationId", out var correlationId))
        {
            diagnosticContext.Set("CorrelationId", correlationId);
        }
    };
});

Log.Information("Order API starting up...");

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
