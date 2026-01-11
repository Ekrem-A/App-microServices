using Catalog.Domain.Common;

namespace Catalog.Domain.Entities;

public class Brand : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    // Navigation Properties
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}