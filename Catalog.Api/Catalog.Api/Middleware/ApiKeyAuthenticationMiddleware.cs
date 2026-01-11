using Microsoft.Extensions.Options;

namespace Catalog.Api.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApiKeySettings _settings;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

    public ApiKeyAuthenticationMiddleware(
        RequestDelegate next,
        IOptions<ApiKeySettings> settings,
        ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication for health check and swagger
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path.Contains("/health") || 
            path.Contains("/swagger") || 
            path.Contains("/api-docs"))
        {
            await _next(context);
            return;
        }

        // Check if API Key authentication is enabled
        if (!_settings.Enabled)
        {
            await _next(context);
            return;
        }

        // Get API Key from header
        if (!context.Request.Headers.TryGetValue(_settings.HeaderName, out var extractedApiKey))
        {
            _logger.LogWarning("API Key missing from request. IP: {IP}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized",
                message = "API Key is missing"
            });
            return;
        }

        // Validate API Key
        var apiKey = extractedApiKey.ToString();
        var validKey = _settings.ValidKeys.FirstOrDefault(k => k.Key == apiKey);

        if (validKey == null)
        {
            _logger.LogWarning("Invalid API Key attempted. IP: {IP}, Key: {Key}", 
                context.Connection.RemoteIpAddress, 
                apiKey.Substring(0, Math.Min(8, apiKey.Length)) + "...");
            
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized",
                message = "Invalid API Key"
            });
            return;
        }

        // Add service name to context for logging
        context.Items["ServiceName"] = validKey.ServiceName;
        _logger.LogInformation("API request from service: {ServiceName}", validKey.ServiceName);

        await _next(context);
    }
}

public class ApiKeySettings
{
    public bool Enabled { get; set; } = true;
    public string HeaderName { get; set; } = "X-API-Key";
    public List<ApiKeyConfig> ValidKeys { get; set; } = new();
}

public class ApiKeyConfig
{
    public string Key { get; set; } = default!;
    public string ServiceName { get; set; } = default!;
    public string Description { get; set; } = default!;
}

