using Cart.Application.DTOs;
using MediatR;

namespace Cart.Application.Carts.Queries.GetCart;

public record GetCartQuery(string UserId) : IRequest<CartDto?>;

