using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Order.Application.Orders.Commands.CancelOrder;
using Order.Application.Orders.Commands.CreateOrder;
using Order.Application.Orders.DTOs;
using Order.Application.Orders.Queries.GetOrder;
using Order.Application.Orders.Queries.GetOrders;
using Order.Domain.Enums;

namespace Order.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new order
    /// </summary>
    /// <param name="command">Order creation details</param>
    /// <param name="idempotencyKey">Optional idempotency key to prevent duplicate orders</param>
    /// <returns>The created order</returns>
    [HttpPost]
    [Authorize(Policy = "Orders.Write")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OrderDto>> CreateOrder(
        [FromBody] CreateOrderCommand command,
        [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        // Set idempotency key from header if not provided in body
        if (!string.IsNullOrWhiteSpace(idempotencyKey) && string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            command = command with { IdempotencyKey = idempotencyKey };
        }

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails
            {
                Title = "Order Creation Failed",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });

        return CreatedAtAction(
            nameof(GetOrder),
            new { id = result.Value.Id },
            result.Value);
    }

    /// <summary>
    /// Gets a specific order by ID
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <returns>The order details</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Orders.Read")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OrderDto>> GetOrder(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetOrderQuery(id), cancellationToken);

        if (result.IsFailure)
            return NotFound(new ProblemDetails
            {
                Title = "Order Not Found",
                Detail = result.Error,
                Status = StatusCodes.Status404NotFound
            });

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets all orders for the current user
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 50)</param>
    /// <param name="status">Optional filter by order status</param>
    /// <returns>Paginated list of orders</returns>
    [HttpGet]
    [Authorize(Policy = "Orders.Read")]
    [ProducesResponseType(typeof(PaginatedList<OrderSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedList<OrderSummaryDto>>> GetOrders(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] OrderStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Min(pageSize, 50); // Max page size

        var result = await _mediator.Send(new GetOrdersQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Status = status
        }, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails
            {
                Title = "Failed to retrieve orders",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });

        return Ok(result.Value);
    }

    /// <summary>
    /// Cancels an order
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="reason">Optional cancellation reason</param>
    /// <returns>No content on success</returns>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "Orders.Write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CancelOrder(
        Guid id,
        [FromQuery] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new CancelOrderCommand(id, reason), cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == "Order not found")
                return NotFound(new ProblemDetails
                {
                    Title = "Order Not Found",
                    Detail = result.Error,
                    Status = StatusCodes.Status404NotFound
                });

            return BadRequest(new ProblemDetails
            {
                Title = "Cancellation Failed",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return NoContent();
    }
}

