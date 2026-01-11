using Cart.Application.DTOs;
using MediatR;

namespace Cart.Application.Carts.Commands.AddItem;

public record AddItemCommand(
    string UserId,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    string? ImageUrl = null
) : IRequest<CartDto>;

