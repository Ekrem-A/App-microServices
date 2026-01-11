namespace Catalog.Application.DTOs;

public record ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    
    // Brand Info
    public Guid BrandId { get; init; }
    public string BrandName { get; init; } = default!;
    public string? BrandLogoUrl { get; init; }
    
    // Category Info
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    
    // Pricing
    public decimal Price { get; init; }
    public decimal? OriginalPrice { get; init; }
    public int DiscountPercentage { get; init; }
    public decimal? DiscountedPrice => DiscountPercentage > 0 
        ? Price * (1 - DiscountPercentage / 100m) 
        : null;
    
    // Description
    public string? Description { get; init; }
    
    // Stock
    public bool InStock { get; init; }
    public int StockQuantity { get; init; }
    
    // Rating & Reviews
    public decimal Rating { get; init; }
    public int ReviewCount { get; init; }
    
    // Images
    public List<string>? Images { get; init; }
    public string? MainImage => Images?.FirstOrDefault();
    
    // Tags
    public List<string>? Tags { get; init; }
    
    // Campaign
    public bool Featured { get; init; }
    public bool IsCampaign { get; init; }
    public DateTimeOffset? CampaignEndDate { get; init; }
    public bool IsCampaignActive => IsCampaign 
        && CampaignEndDate.HasValue 
        && CampaignEndDate.Value > DateTimeOffset.UtcNow;
    
    // Source
    public string? ProductSource { get; init; }
    
    // Timestamps
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
