using Order.Application.Orders.DTOs;

namespace Order.Application.Common.Interfaces;

/// <summary>
/// Service for sending real-time order notifications via SignalR
/// </summary>
public interface IOrderNotificationService
{
    /// <summary>
    /// Notifies all connected clients about a new order
    /// </summary>
    Task NotifyOrderCreatedAsync(OrderDto order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies all connected clients about an order cancellation
    /// </summary>
    Task NotifyOrderCancelledAsync(Guid orderId, string userId, string? reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies all connected clients about an order status change
    /// </summary>
    Task NotifyOrderStatusChangedAsync(Guid orderId, string userId, string newStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies a specific user about their order update
    /// </summary>
    Task NotifyUserOrderUpdatedAsync(string userId, OrderDto order, CancellationToken cancellationToken = default);
}
