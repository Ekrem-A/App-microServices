namespace Catalog.Application.DTOs;

public record BrandDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string? Description { get; init; }
    public string? LogoUrl { get; init; }
    public string? WebsiteUrl { get; init; }
    public bool IsActive { get; init; }
    public int DisplayOrder { get; init; }
    public int ProductCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
