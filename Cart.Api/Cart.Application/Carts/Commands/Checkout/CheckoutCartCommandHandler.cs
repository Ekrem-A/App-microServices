using Cart.Application.Abstractions;
using Cart.Application.DTOs;
using Cart.Application.Events;
using MediatR;

namespace Cart.Application.Carts.Commands.Checkout;

public class CheckoutCartCommandHandler : IRequestHandler<CheckoutCartCommand, CartDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly IEventPublisher _eventPublisher;

    public CheckoutCartCommandHandler(
        ICartRepository cartRepository,
        IEventPublisher eventPublisher)
    {
        _cartRepository = cartRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<CartDto> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetCartAsync(request.UserId, cancellationToken);

        if (cart is null || cart.Items.Count == 0)
            throw new InvalidOperationException($"Cart is empty or not found for user {request.UserId}.");

        var cartDto = cart.ToDto();

        var checkoutEvent = CheckoutRequestedEvent.Create(
            request.UserId,
            cartDto,
            request.ShippingAddress,
            request.PaymentMethod
        );

        await _eventPublisher.PublishAsync(checkoutEvent, cancellationToken);

        // Clear the cart after successful checkout
        await _cartRepository.DeleteCartAsync(request.UserId, cancellationToken);

        return cartDto;
    }
}

