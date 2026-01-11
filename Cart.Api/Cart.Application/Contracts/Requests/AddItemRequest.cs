namespace Cart.Application.Contracts.Requests;

public record AddItemRequest(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    string? ImageUrl = null
);

