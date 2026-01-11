using Cart.Application.DTOs;
using MediatR;

namespace Cart.Application.Carts.Commands.RemoveCoupon;

public record RemoveCouponCommand(string UserId) : IRequest<CartDto>;

