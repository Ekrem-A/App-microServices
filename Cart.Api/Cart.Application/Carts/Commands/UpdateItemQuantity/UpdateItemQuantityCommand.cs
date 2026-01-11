using Cart.Application.DTOs;
using MediatR;

namespace Cart.Application.Carts.Commands.UpdateItemQuantity;

public record UpdateItemQuantityCommand(
    string UserId,
    Guid ProductId,
    int NewQuantity
) : IRequest<CartDto>;

