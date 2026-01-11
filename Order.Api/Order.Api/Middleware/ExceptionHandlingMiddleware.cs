using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Order.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
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
        var problemDetails = exception switch
        {
            ValidationException validationException => CreateValidationProblemDetails(context, validationException),
            UnauthorizedAccessException => CreateProblemDetails(
                context,
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "You do not have permission to access this resource."),
            InvalidOperationException invalidOpEx => CreateProblemDetails(
                context,
                StatusCodes.Status400BadRequest,
                "Bad Request",
                invalidOpEx.Message),
            ArgumentException argEx => CreateProblemDetails(
                context,
                StatusCodes.Status400BadRequest,
                "Bad Request",
                argEx.Message),
            _ => CreateProblemDetails(
                context,
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                _environment.IsDevelopment() ? exception.Message : "An unexpected error occurred.")
        };

        // Log the exception
        if (problemDetails.Status >= 500)
        {
            _logger.LogError(exception, "An unhandled exception occurred. TraceId: {TraceId}", problemDetails.Extensions["traceId"]);
        }
        else
        {
            _logger.LogWarning(exception, "A handled exception occurred. TraceId: {TraceId}", problemDetails.Extensions["traceId"]);
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private static ProblemDetails CreateProblemDetails(
        HttpContext context,
        int statusCode,
        string title,
        string detail)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = context.TraceIdentifier
            }
        };
    }

    private static ProblemDetails CreateValidationProblemDetails(
        HttpContext context,
        ValidationException validationException)
    {
        var errors = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Failed",
            Detail = "One or more validation errors occurred.",
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = context.TraceIdentifier
            }
        };
    }
}

