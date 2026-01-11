using Cart.Application.Carts.Commands.AddItem;
using Cart.Application.Carts.Commands.ApplyCoupon;
using Cart.Application.Carts.Commands.Checkout;
using Cart.Application.Carts.Commands.ClearCart;
using Cart.Application.Carts.Commands.MergeCart;
using Cart.Application.Carts.Commands.RemoveCoupon;
using Cart.Application.Carts.Commands.RemoveItem;
using Cart.Application.Carts.Commands.RepriceCart;
using Cart.Application.Carts.Commands.UpdateItemQuantity;
using Cart.Application.Carts.Queries.GetCart;
using Cart.Application.Contracts.Requests;
using Cart.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Cart.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CartController> _logger;

    public CartController(IMediator mediator, ILogger<CartController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get cart by user ID
    /// </summary>
    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> GetCart(string userId, CancellationToken cancellationToken)
    {
        var cart = await _mediator.Send(new GetCartQuery(userId), cancellationToken);

        if (cart is null)
            return NotFound(new { message = $"Cart not found for user {userId}" });

        return Ok(cart);
    }

    /// <summary>
    /// Add item to cart
    /// </summary>
    [HttpPost("{userId}/items")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CartDto>> AddItem(
        string userId,
        [FromBody] AddItemRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddItemCommand(
            userId,
            request.ProductId,
            request.ProductName,
            request.UnitPrice,
            request.Quantity,
            request.ImageUrl
        );

        var cart = await _mediator.Send(command, cancellationToken);
        return Ok(cart);
    }

    /// <summary>
    /// Update item quantity
    /// </summary>
    [HttpPut("{userId}/items/{productId:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> UpdateItemQuantity(
        string userId,
        Guid productId,
        [FromBody] UpdateItemQuantityRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateItemQuantityCommand(userId, productId, request.NewQuantity);
        var cart = await _mediator.Send(command, cancellationToken);
        return Ok(cart);
    }

    /// <summary>
    /// Remove item from cart
    /// </summary>
    [HttpDelete("{userId}/items/{productId:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> RemoveItem(
        string userId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var command = new RemoveItemCommand(userId, productId);
        var cart = await _mediator.Send(command, cancellationToken);
        return Ok(cart);
    }

    /// <summary>
    /// Clear cart
    /// </summary>
    [HttpDelete("{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearCart(string userId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ClearCartCommand(userId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Checkout cart (publishes event to Kafka)
    /// </summary>
    [HttpPost("{userId}/checkout")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> Checkout(
        string userId,
        [FromBody] CheckoutRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new CheckoutCartCommand(
            userId,
            request?.ShippingAddress,
            request?.PaymentMethod
        );

        var cart = await _mediator.Send(command, cancellationToken);
        return Ok(cart);
    }

    /// <summary>
    /// Merge anonymous cart into user cart
    /// </summary>
    [HttpPost("{userId}/merge")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CartDto>> MergeCart(
        string userId,
        [FromBody] MergeCartRequest request,
        CancellationToken cancellationToken)
    {
        var command = new MergeCartCommand(userId, request.SourceUserId);
        var cart = await _mediator.Send(command, cancellationToken);
        return Ok(cart);
    }

    /// <summary>
    /// Apply coupon to cart
    /// </summary>
    [HttpPost("{userId}/coupon")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> ApplyCoupon(
        string userId,
        [FromBody] ApplyCouponRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ApplyCouponCommand(
            userId,
            request.CouponCode,
            request.CouponType,
            request.Value,
            request.MinimumOrderAmount,
            request.ExpiresAt
        );

        var cart = await _mediator.Send(command, cancellationToken);
        return Ok(cart);
    }

    /// <summary>
    /// Remove coupon from cart
    /// </summary>
    [HttpDelete("{userId}/coupon")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> RemoveCoupon(
        string userId,
        CancellationToken cancellationToken)
    {
        var command = new RemoveCouponCommand(userId);
        var cart = await _mediator.Send(command, cancellationToken);
        return Ok(cart);
    }

    /// <summary>
    /// Reprice cart items (for catalog price sync)
    /// </summary>
    [HttpPost("{userId}/reprice")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> RepriceCart(
        string userId,
        [FromBody] RepriceCartRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RepriceCartCommand(
            userId,
            request.PriceUpdates.Select(p =>
                new ProductPriceUpdate(p.ProductId, p.NewUnitPrice)
            ).ToList()
        );

        var cart = await _mediator.Send(command, cancellationToken);
        return Ok(cart);
    }
}

