using Cart.Application.DTOs;
using MediatR;

namespace Cart.Application.Carts.Commands.MergeCart;

public record MergeCartCommand(
    string TargetUserId,
    string SourceUserId
) : IRequest<CartDto>;

