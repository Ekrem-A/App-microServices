using System.Text.Json;
using Cart.Domain.CartAggregate;

namespace Cart.Infrastructure.Persistence;

public static class CartSerializationHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string Serialize(ShoppingCart cart)
    {
        var dto = new CartPersistenceDto
        {
            UserId = cart.UserId,
            Items = cart.Items.Select(i => new CartItemPersistenceDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                ImageUrl = i.ImageUrl,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList(),
            AppliedCoupon = cart.AppliedCoupon is not null
                ? new CouponPersistenceDto
                {
                    Code = cart.AppliedCoupon.Code,
                    Type = cart.AppliedCoupon.Type.ToString(),
                    Value = cart.AppliedCoupon.Value,
                    MinimumOrderAmount = cart.AppliedCoupon.MinimumOrderAmount,
                    ExpiresAt = cart.AppliedCoupon.ExpiresAt
                }
                : null,
            CreatedAt = cart.CreatedAt,
            UpdatedAt = cart.UpdatedAt
        };

        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    public static ShoppingCart? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        var dto = JsonSerializer.Deserialize<CartPersistenceDto>(json, JsonOptions);
        if (dto is null)
            return null;

        var cart = new ShoppingCart(dto.UserId);

        foreach (var item in dto.Items)
        {
            cart.AddItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity, item.ImageUrl);
        }

        if (dto.AppliedCoupon is not null)
        {
            var couponType = Enum.Parse<CouponType>(dto.AppliedCoupon.Type, ignoreCase: true);
            var coupon = new Coupon(
                dto.AppliedCoupon.Code,
                couponType,
                dto.AppliedCoupon.Value,
                dto.AppliedCoupon.MinimumOrderAmount,
                dto.AppliedCoupon.ExpiresAt
            );
            cart.ApplyCoupon(coupon);
        }

        return cart;
    }
}

internal class CartPersistenceDto
{
    public string UserId { get; set; } = string.Empty;
    public List<CartItemPersistenceDto> Items { get; set; } = new();
    public CouponPersistenceDto? AppliedCoupon { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

internal class CartItemPersistenceDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

internal class CouponPersistenceDto
{
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

