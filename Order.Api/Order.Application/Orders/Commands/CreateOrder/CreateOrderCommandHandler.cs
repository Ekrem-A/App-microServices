using MediatR;
using Microsoft.EntityFrameworkCore;
using Order.Application.Common.Interfaces;
using Order.Application.Common.Models;
using Order.Application.Orders.DTOs;
using Order.Application.Orders.Mappers;
using Order.Domain.ValueObjects;

namespace Order.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<OrderDto>>
{
    private readonly IOrderDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateOrderCommandHandler(
        IOrderDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<OrderDto>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId))
            return Result.Failure<OrderDto>("User is not authenticated");

        // Check for idempotency
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existingOrder = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(
                    o => o.IdempotencyKey == request.IdempotencyKey && o.UserId == _currentUser.UserId,
                    cancellationToken);

            if (existingOrder != null)
            {
                return Result.Success(OrderMapper.ToDto(existingOrder));
            }
        }

        // Create shipping address
        var shippingAddress = Address.Create(
            request.ShippingAddress.Street,
            request.ShippingAddress.City,
            request.ShippingAddress.District,
            request.ShippingAddress.PostalCode,
            request.ShippingAddress.Country,
            request.ShippingAddress.BuildingNumber,
            request.ShippingAddress.ApartmentNumber);

        // Create billing address if provided
        Address? billingAddress = null;
        if (request.BillingAddress != null)
        {
            billingAddress = Address.Create(
                request.BillingAddress.Street,
                request.BillingAddress.City,
                request.BillingAddress.District,
                request.BillingAddress.PostalCode,
                request.BillingAddress.Country,
                request.BillingAddress.BuildingNumber,
                request.BillingAddress.ApartmentNumber);
        }

        // Create order
        var order = Domain.Entities.Order.Create(
            _currentUser.UserId,
            shippingAddress,
            billingAddress,
            request.Notes,
            request.IdempotencyKey);

        // Add items
        foreach (var item in request.Items)
        {
            var unitPrice = Money.Create(item.UnitPrice);
            order.AddItem(
                item.ProductId,
                item.ProductName,
                item.ProductImageUrl,
                item.Quantity,
                unitPrice);
        }

        // Confirm the order (this will trigger domain events)
        order.Confirm();

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(OrderMapper.ToDto(order));
    }
}

