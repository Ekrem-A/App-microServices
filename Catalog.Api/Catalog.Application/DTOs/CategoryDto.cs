namespace Catalog.Application.DTOs;

public record CategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string? Level { get; init; }
    public Guid? ParentId { get; init; }
    public string? ParentName { get; init; }
    public string? Description { get; init; }
    public int DisplayOrder { get; init; }
    public int ProductCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
