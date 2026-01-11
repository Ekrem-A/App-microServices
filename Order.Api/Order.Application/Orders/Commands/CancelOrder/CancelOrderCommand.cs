using MediatR;
using Order.Application.Common.Models;

namespace Order.Application.Orders.Commands.CancelOrder;

public record CancelOrderCommand(Guid OrderId, string? Reason = null) : IRequest<Result>;

