using MediatR;
using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces;
using System.Diagnostics;

namespace Order.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUser;

    public LoggingBehaviour(
        ILogger<LoggingBehaviour<TRequest, TResponse>> logger,
        ICurrentUserService currentUser)
    {
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _currentUser.UserId ?? "Anonymous";

        _logger.LogInformation(
            "Order Service Request: {Name} {@UserId} {@Request}",
            requestName, userId, request);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();

            stopwatch.Stop();

            _logger.LogInformation(
                "Order Service Request Completed: {Name} {@UserId} - {ElapsedMilliseconds}ms",
                requestName, userId, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Order Service Request Failed: {Name} {@UserId} - {ElapsedMilliseconds}ms - {Error}",
                requestName, userId, stopwatch.ElapsedMilliseconds, ex.Message);

            throw;
        }
    }
}

