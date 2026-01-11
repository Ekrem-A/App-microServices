using System.Text.Json;
using FluentValidation;

namespace Cart.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var (statusCode, message, errors) = exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                validationEx.Errors.Select(e => new { field = e.PropertyName, error = e.ErrorMessage })
            ),
            InvalidOperationException invalidOpEx => (
                StatusCodes.Status400BadRequest,
                invalidOpEx.Message,
                (object?)null
            ),
            ArgumentException argEx => (
                StatusCodes.Status400BadRequest,
                argEx.Message,
                (object?)null
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                (object?)null
            )
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred");
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception: {Message}", exception.Message);
        }

        response.StatusCode = statusCode;

        var result = JsonSerializer.Serialize(new
        {
            status = statusCode,
            message,
            errors
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await response.WriteAsync(result);
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}

