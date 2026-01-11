using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Categories.Commands.CreateCategory;

public record CreateCategoryCommand : IRequest<CategoryDto>
{
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string? Level { get; init; }
    public Guid? ParentId { get; init; }
    public string? Description { get; init; }
    public int DisplayOrder { get; init; }
}