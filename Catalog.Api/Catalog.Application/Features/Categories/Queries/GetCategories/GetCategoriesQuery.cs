using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Features.Categories.Queries.GetCategories;

public record GetCategoriesQuery : IRequest<List<CategoryDto>>
{
    public bool? IsActive { get; init; }
    public Guid? ParentId { get; init; }
}