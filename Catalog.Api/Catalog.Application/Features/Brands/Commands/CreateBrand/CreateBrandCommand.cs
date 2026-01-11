using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Brands.Commands.CreateBrand;

public record CreateBrandCommand : IRequest<BrandDto>
{
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string? Description { get; init; }
    public string? LogoUrl { get; init; }
    public string? WebsiteUrl { get; init; }
    public int DisplayOrder { get; init; }
}