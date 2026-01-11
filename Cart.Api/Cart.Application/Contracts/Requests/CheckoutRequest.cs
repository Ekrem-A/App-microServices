namespace Cart.Application.Contracts.Requests;

public record CheckoutRequest(
    string? ShippingAddress = null,
    string? PaymentMethod = null
);

