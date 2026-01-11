using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net;

namespace Catalog.Api.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly RateLimitSettings _settings;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        IOptions<RateLimitSettings> settings,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_settings.Enabled)
        {
            await _next(context);
            return;
        }

        var endpoint = context.GetEndpoint();
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        // Skip rate limiting for health check
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        var key = $"rate_limit_{remoteIp}";
        
        if (!_cache.TryGetValue(key, out RequestCounter counter))
        {
            counter = new RequestCounter
            {
                Count = 0,
                WindowStart = DateTime.UtcNow
            };
        }

        // Reset counter if window expired
        if ((DateTime.UtcNow - counter.WindowStart).TotalSeconds >= _settings.WindowSeconds)
        {
            counter.Count = 0;
            counter.WindowStart = DateTime.UtcNow;
        }

        counter.Count++;

        // Check if limit exceeded
        if (counter.Count > _settings.MaxRequestsPerWindow)
        {
            _logger.LogWarning(
                "Rate limit exceeded for IP: {IP}. Count: {Count}, Limit: {Limit}",
                remoteIp, counter.Count, _settings.MaxRequestsPerWindow);

            context.Response.StatusCode = 429; // Too Many Requests
            context.Response.Headers.Add("Retry-After", _settings.WindowSeconds.ToString());
            
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Too Many Requests",
                message = $"Rate limit exceeded. Maximum {_settings.MaxRequestsPerWindow} requests per {_settings.WindowSeconds} seconds.",
                retryAfter = _settings.WindowSeconds
            });
            return;
        }

        // Save updated counter
        _cache.Set(key, counter, TimeSpan.FromSeconds(_settings.WindowSeconds));

        // Add rate limit headers
        context.Response.Headers.Add("X-RateLimit-Limit", _settings.MaxRequestsPerWindow.ToString());
        context.Response.Headers.Add("X-RateLimit-Remaining", (_settings.MaxRequestsPerWindow - counter.Count).ToString());
        context.Response.Headers.Add("X-RateLimit-Reset", counter.WindowStart.AddSeconds(_settings.WindowSeconds).ToString("O"));

        await _next(context);
    }

    private class RequestCounter
    {
        public int Count { get; set; }
        public DateTime WindowStart { get; set; }
    }
}

public class RateLimitSettings
{
    public bool Enabled { get; set; } = true;
    public int MaxRequestsPerWindow { get; set; } = 100;
    public int WindowSeconds { get; set; } = 60;
}

