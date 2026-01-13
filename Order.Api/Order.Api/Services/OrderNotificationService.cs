using Microsoft.AspNetCore.SignalR;
using Order.Api.Hubs;
using Order.Application.Common.Interfaces;
using Order.Application.Orders.DTOs;

namespace Order.Api.Services;

/// <summary>
/// SignalR implementation of order notification service
/// </summary>
public class OrderNotificationService : IOrderNotificationService
{
    private readonly IHubContext<OrderHub> _hubContext;
    private readonly ILogger<OrderNotificationService> _logger;

    public OrderNotificationService(
        IHubContext<OrderHub> hubContext,
        ILogger<OrderNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyOrderCreatedAsync(OrderDto order, CancellationToken cancellationToken = default)
    {
        try
        {
            // Notify admins watching all orders
            await _hubContext.Clients.Group("all_orders")
                .SendAsync(OrderHubMethods.OrderCreated, order, cancellationToken);

            // Notify the specific user
            await _hubContext.Clients.Group($"user_{order.UserId}")
                .SendAsync(OrderHubMethods.OrderCreated, order, cancellationToken);

            _logger.LogInformation("Order created notification sent for OrderId: {OrderId}, UserId: {UserId}",
                order.Id, order.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order created notification for OrderId: {OrderId}", order.Id);
        }
    }

    public async Task NotifyOrderCancelledAsync(Guid orderId, string userId, string? reason, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = new OrderCancelledNotification(orderId, userId, reason, DateTime.UtcNow);

            // Notify admins
            await _hubContext.Clients.Group("all_orders")
                .SendAsync(OrderHubMethods.OrderCancelled, notification, cancellationToken);

            // Notify subscribers of this specific order
            await _hubContext.Clients.Group($"order_{orderId}")
                .SendAsync(OrderHubMethods.OrderCancelled, notification, cancellationToken);

            // Notify the user
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync(OrderHubMethods.OrderCancelled, notification, cancellationToken);

            _logger.LogInformation("Order cancelled notification sent for OrderId: {OrderId}, UserId: {UserId}",
                orderId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order cancelled notification for OrderId: {OrderId}", orderId);
        }
    }

    public async Task NotifyOrderStatusChangedAsync(Guid orderId, string userId, string newStatus, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = new OrderStatusChangedNotification(orderId, userId, newStatus, DateTime.UtcNow);

            // Notify admins
            await _hubContext.Clients.Group("all_orders")
                .SendAsync(OrderHubMethods.OrderStatusChanged, notification, cancellationToken);

            // Notify subscribers of this specific order
            await _hubContext.Clients.Group($"order_{orderId}")
                .SendAsync(OrderHubMethods.OrderStatusChanged, notification, cancellationToken);

            // Notify the user
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync(OrderHubMethods.OrderStatusChanged, notification, cancellationToken);

            _logger.LogInformation("Order status changed notification sent for OrderId: {OrderId}, NewStatus: {Status}",
                orderId, newStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order status changed notification for OrderId: {OrderId}", orderId);
        }
    }

    public async Task NotifyUserOrderUpdatedAsync(string userId, OrderDto order, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync(OrderHubMethods.OrderUpdated, order, cancellationToken);

            _logger.LogInformation("Order updated notification sent to UserId: {UserId}, OrderId: {OrderId}",
                userId, order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order updated notification for UserId: {UserId}", userId);
        }
    }
}

/// <summary>
/// Notification payload for order cancellation
/// </summary>
public record OrderCancelledNotification(
    Guid OrderId,
    string UserId,
    string? Reason,
    DateTime CancelledAtUtc);

/// <summary>
/// Notification payload for order status change
/// </summary>
public record OrderStatusChangedNotification(
    Guid OrderId,
    string UserId,
    string NewStatus,
    DateTime ChangedAtUtc);
