using MediatR;

namespace Cart.Application.Carts.Commands.ClearCart;

public record ClearCartCommand(string UserId) : IRequest<Unit>;

