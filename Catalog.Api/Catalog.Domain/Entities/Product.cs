using Catalog.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Catalog.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;

    // Brand Relationship
    public Guid BrandId { get; set; }
    public virtual Brand Brand { get; set; } = default!;

    // Category Relationship
    public Guid? CategoryId { get; set; }
    public virtual Category? Category { get; set; }

    // Pricing
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }

    // Description
    public string? Description { get; set; }

    // Stock
    public bool InStock { get; set; } = true;
    public int StockQuantity { get; set; }

    // Rating & Reviews
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }

    // Images (JSON array stored as string, will be converted to List<string> in application layer)
    public string? Images { get; set; } // JSON array: ["url1", "url2", "url3"]
    public string? Tags { get; set; }   // JSON array: ["tag1", "tag2"]

    // Campaign
    public bool Featured { get; set; }
    public bool IsCampaign { get; set; }
    public int DiscountPercentage { get; set; }
    public DateTimeOffset? CampaignEndDate { get; set; }

    // Product Source
    public string? ProductSource { get; set; } = "own"; // "own" | "supplier"

    // Dates
    public DateTimeOffset? LastSyncedAt { get; set; }
}