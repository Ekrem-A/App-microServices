using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Order.Application.Orders.DTOs;

namespace Order.Api.Hubs;

/// <summary>
/// SignalR Hub for real-time order notifications
/// </summary>
[Authorize]
public class OrderHub : Hub
{
    private readonly ILogger<OrderHub> _logger;

    public OrderHub(ILogger<OrderHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("sub")?.Value 
            ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            // Add user to their personal group for targeted notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} connected to OrderHub. ConnectionId: {ConnectionId}", 
                userId, Context.ConnectionId);
        }

        // Add to admin group if user has admin role
        if (Context.User?.IsInRole("Admin") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
            _logger.LogInformation("Admin user connected to OrderHub. ConnectionId: {ConnectionId}", 
                Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst("sub")?.Value 
            ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("User {UserId} disconnected from OrderHub. ConnectionId: {ConnectionId}", 
            userId, Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Allows clients to subscribe to order updates for a specific order
    /// </summary>
    public async Task SubscribeToOrder(Guid orderId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"order_{orderId}");
        _logger.LogDebug("ConnectionId {ConnectionId} subscribed to order {OrderId}", 
            Context.ConnectionId, orderId);
    }

    /// <summary>
    /// Allows clients to unsubscribe from order updates
    /// </summary>
    public async Task UnsubscribeFromOrder(Guid orderId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order_{orderId}");
        _logger.LogDebug("ConnectionId {ConnectionId} unsubscribed from order {OrderId}", 
            Context.ConnectionId, orderId);
    }

    /// <summary>
    /// Allows admins to subscribe to all order updates
    /// </summary>
    public async Task SubscribeToAllOrders()
    {
        if (Context.User?.IsInRole("Admin") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "all_orders");
            _logger.LogInformation("Admin subscribed to all orders. ConnectionId: {ConnectionId}", 
                Context.ConnectionId);
        }
    }
}

/// <summary>
/// Client method names for SignalR notifications
/// </summary>
public static class OrderHubMethods
{
    public const string OrderCreated = "OrderCreated";
    public const string OrderCancelled = "OrderCancelled";
    public const string OrderStatusChanged = "OrderStatusChanged";
    public const string OrderUpdated = "OrderUpdated";
}
