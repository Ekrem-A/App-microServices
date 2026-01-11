using Cart.Application.DTOs;

namespace Cart.Application.Events;

public record CheckoutRequestedEvent(
    Guid EventId,
    string UserId,
    CartDto Cart,
    string? ShippingAddress,
    string? PaymentMethod,
    DateTime OccurredAt
)
{
    public static CheckoutRequestedEvent Create(
        string userId, 
        CartDto cart, 
        string? shippingAddress = null, 
        string? paymentMethod = null)
    {
        return new CheckoutRequestedEvent(
            EventId: Guid.NewGuid(),
            UserId: userId,
            Cart: cart,
            ShippingAddress: shippingAddress,
            PaymentMethod: paymentMethod,
            OccurredAt: DateTime.UtcNow
        );
    }
}

