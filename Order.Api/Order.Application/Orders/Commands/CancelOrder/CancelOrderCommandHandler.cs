using MediatR;
using Microsoft.EntityFrameworkCore;
using Order.Application.Common.Interfaces;
using Order.Application.Common.Models;

namespace Order.Application.Orders.Commands.CancelOrder;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result>
{
    private readonly IOrderDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CancelOrderCommandHandler(
        IOrderDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId))
            return Result.Failure("User is not authenticated");

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
            return Result.Failure("Order not found");

        // User can only cancel their own orders (unless admin)
        if (order.UserId != _currentUser.UserId && !_currentUser.HasRole("Admin"))
            return Result.Failure("You are not authorized to cancel this order");

        try
        {
            order.Cancel(request.Reason);
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}

