using Order.Domain.Enums;

namespace Order.Application.Orders.DTOs;

public record OrderDto(
    Guid Id,
    string UserId,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    AddressDto ShippingAddress,
    AddressDto? BillingAddress,
    IEnumerable<OrderItemDto> Items,
    decimal SubTotal,
    decimal ShippingCost,
    decimal TotalAmount,
    string Currency,
    string? Notes,
    string? TrackingNumber,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? ShippedAtUtc,
    DateTime? DeliveredAtUtc);

public record OrderItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? ProductImageUrl,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice);

public record AddressDto(
    string Street,
    string City,
    string District,
    string PostalCode,
    string Country,
    string? BuildingNumber,
    string? ApartmentNumber);

public record OrderSummaryDto(
    Guid Id,
    OrderStatus Status,
    int ItemCount,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAtUtc);

