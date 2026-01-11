namespace Cart.Application.Contracts.Requests;

public record RepriceCartRequest(List<ProductPriceUpdateRequest> PriceUpdates);

public record ProductPriceUpdateRequest(Guid ProductId, decimal NewUnitPrice);

