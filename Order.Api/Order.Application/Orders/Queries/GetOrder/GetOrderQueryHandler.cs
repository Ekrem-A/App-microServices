using MediatR;
using Microsoft.EntityFrameworkCore;
using Order.Application.Common.Interfaces;
using Order.Application.Common.Models;
using Order.Application.Orders.DTOs;
using Order.Application.Orders.Mappers;

namespace Order.Application.Orders.Queries.GetOrder;

public class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, Result<OrderDto>>
{
    private readonly IOrderDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetOrderQueryHandler(
        IOrderDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<OrderDto>> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId))
            return Result.Failure<OrderDto>("User is not authenticated");

        var order = await _context.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
            return Result.Failure<OrderDto>("Order not found");

        // User can only view their own orders (unless admin)
        if (order.UserId != _currentUser.UserId && !_currentUser.HasRole("Admin"))
            return Result.Failure<OrderDto>("You are not authorized to view this order");

        return Result.Success(OrderMapper.ToDto(order));
    }
}

