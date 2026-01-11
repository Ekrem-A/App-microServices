using MediatR;
using Order.Application.Common.Models;
using Order.Application.Orders.DTOs;

namespace Order.Application.Orders.Queries.GetOrder;

public record GetOrderQuery(Guid OrderId) : IRequest<Result<OrderDto>>;

