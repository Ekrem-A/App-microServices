using Order.Application.Orders.DTOs;
using Order.Domain.Entities;
using Order.Domain.ValueObjects;

namespace Order.Application.Orders.Mappers;

public static class OrderMapper
{
    public static OrderDto ToDto(Domain.Entities.Order order)
    {
        return new OrderDto(
            order.Id,
            order.UserId,
            order.Status,
            order.PaymentStatus,
            ToAddressDto(order.ShippingAddress),
            order.BillingAddress != null ? ToAddressDto(order.BillingAddress) : null,
            order.Items.Select(ToOrderItemDto),
            order.SubTotal.Amount,
            order.ShippingCost.Amount,
            order.TotalAmount.Amount,
            order.TotalAmount.Currency,
            order.Notes,
            order.TrackingNumber,
            order.CreatedAtUtc,
            order.UpdatedAtUtc,
            order.ShippedAtUtc,
            order.DeliveredAtUtc);
    }

    public static OrderSummaryDto ToSummaryDto(Domain.Entities.Order order)
    {
        return new OrderSummaryDto(
            order.Id,
            order.Status,
            order.Items.Count,
            order.TotalAmount.Amount,
            order.TotalAmount.Currency,
            order.CreatedAtUtc);
    }

    public static OrderItemDto ToOrderItemDto(OrderItem item)
    {
        return new OrderItemDto(
            item.Id,
            item.ProductId,
            item.ProductName,
            item.ProductImageUrl,
            item.Quantity,
            item.UnitPrice.Amount,
            item.TotalPrice.Amount);
    }

    public static AddressDto ToAddressDto(Address address)
    {
        return new AddressDto(
            address.Street,
            address.City,
            address.District,
            address.PostalCode,
            address.Country,
            address.BuildingNumber,
            address.ApartmentNumber);
    }
}

