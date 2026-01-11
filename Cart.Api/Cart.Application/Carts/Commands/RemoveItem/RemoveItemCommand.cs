using Cart.Application.DTOs;
using MediatR;

namespace Cart.Application.Carts.Commands.RemoveItem;

public record RemoveItemCommand(
    string UserId,
    Guid ProductId
) : IRequest<CartDto>;

