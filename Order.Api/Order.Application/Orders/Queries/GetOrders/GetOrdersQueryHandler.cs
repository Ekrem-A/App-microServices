using MediatR;
using Microsoft.EntityFrameworkCore;
using Order.Application.Common.Interfaces;
using Order.Application.Common.Models;
using Order.Application.Orders.DTOs;
using Order.Application.Orders.Mappers;

namespace Order.Application.Orders.Queries.GetOrders;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, Result<PaginatedList<OrderSummaryDto>>>
{
    private readonly IOrderDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetOrdersQueryHandler(
        IOrderDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<PaginatedList<OrderSummaryDto>>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId))
            return Result.Failure<PaginatedList<OrderSummaryDto>>("User is not authenticated");

        var query = _context.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == _currentUser.UserId)
            .AsNoTracking();

        if (request.Status.HasValue)
        {
            query = query.Where(o => o.Status == request.Status.Value);
        }

        query = query.OrderByDescending(o => o.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(OrderMapper.ToSummaryDto).ToList();

        return Result.Success(new PaginatedList<OrderSummaryDto>(dtos, totalCount, request.PageNumber, request.PageSize));
    }
}

