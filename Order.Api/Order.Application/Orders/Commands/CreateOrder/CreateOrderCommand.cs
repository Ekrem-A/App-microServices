using MediatR;
using Order.Application.Common.Models;
using Order.Application.Orders.DTOs;

namespace Order.Application.Orders.Commands.CreateOrder;

public record CreateOrderCommand : IRequest<Result<OrderDto>>
{
    public string? IdempotencyKey { get; init; }
    public CreateAddressRequest ShippingAddress { get; init; } = null!;
    public CreateAddressRequest? BillingAddress { get; init; }
    public IEnumerable<CreateOrderItemRequest> Items { get; init; } = Enumerable.Empty<CreateOrderItemRequest>();
    public string? Notes { get; init; }
}

public record CreateAddressRequest(
    string Street,
    string City,
    string District,
    string PostalCode,
    string Country,
    string? BuildingNumber,
    string? ApartmentNumber);

public record CreateOrderItemRequest(
    Guid ProductId,
    string ProductName,
    string? ProductImageUrl,
    int Quantity,
    decimal UnitPrice);

