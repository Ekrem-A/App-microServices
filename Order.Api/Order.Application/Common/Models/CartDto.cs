namespace Order.Application.Common.Models;

public record CartDto(
    string UserId,
    IEnumerable<CartItemDto> Items,
    decimal TotalAmount);

public record CartItemDto(
    Guid ProductId,
    string ProductName,
    string? ProductImageUrl,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice);

