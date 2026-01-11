using Cart.Application.DTOs;
using MediatR;

namespace Cart.Application.Carts.Commands.RepriceCart;

public record RepriceCartCommand(
    string UserId,
    List<ProductPriceUpdate> PriceUpdates
) : IRequest<CartDto>;

public record ProductPriceUpdate(Guid ProductId, decimal NewUnitPrice);

