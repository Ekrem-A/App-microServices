using MediatR;
using Order.Application.Common.Models;
using Order.Application.Orders.DTOs;
using Order.Domain.Enums;

namespace Order.Application.Orders.Queries.GetOrders;

public record GetOrdersQuery : IRequest<Result<PaginatedList<OrderSummaryDto>>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public OrderStatus? Status { get; init; }
}

public class PaginatedList<T>
{
    public IReadOnlyCollection<T> Items { get; }
    public int PageNumber { get; }
    public int TotalPages { get; }
    public int TotalCount { get; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PaginatedList(IReadOnlyCollection<T> items, int count, int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
        Items = items;
    }
}

