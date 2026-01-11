using Cart.Application.DTOs;
using MediatR;

namespace Cart.Application.Carts.Commands.Checkout;

public record CheckoutCartCommand(
    string UserId,
    string? ShippingAddress = null,
    string? PaymentMethod = null
) : IRequest<CartDto>;

