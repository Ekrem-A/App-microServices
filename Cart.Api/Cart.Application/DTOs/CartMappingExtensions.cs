using Cart.Domain.CartAggregate;

namespace Cart.Application.DTOs;

public static class CartMappingExtensions
{
    public static CartDto ToDto(this ShoppingCart cart)
    {
        return new CartDto(
            UserId: cart.UserId,
            Items: cart.Items.Select(i => i.ToDto()).ToList(),
            AppliedCoupon: cart.AppliedCoupon?.ToDto(),
            SubTotal: cart.SubTotal,
            Discount: cart.Discount,
            Total: cart.Total,
            TotalItemCount: cart.TotalItemCount,
            CreatedAt: cart.CreatedAt,
            UpdatedAt: cart.UpdatedAt
        );
    }

    public static CartItemDto ToDto(this CartItem item)
    {
        return new CartItemDto(
            ProductId: item.ProductId,
            ProductName: item.ProductName,
            ImageUrl: item.ImageUrl,
            UnitPrice: item.UnitPrice,
            Quantity: item.Quantity,
            TotalPrice: item.TotalPrice
        );
    }

    public static CouponDto ToDto(this Coupon coupon)
    {
        return new CouponDto(
            Code: coupon.Code,
            Type: coupon.Type.ToString(),
            Value: coupon.Value,
            MinimumOrderAmount: coupon.MinimumOrderAmount,
            ExpiresAt: coupon.ExpiresAt
        );
    }
}

