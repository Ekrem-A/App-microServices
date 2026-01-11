using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Features.Categories.Commands.Update;

public record UpdateCategoryCommand : IRequest<CategoryDto>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string? Level { get; init; }
    public Guid? ParentId { get; init; }
    public string? ParentName { get; init; }
    public string? Description { get; init; }
    public int DisplayOrder { get; init; }
}
