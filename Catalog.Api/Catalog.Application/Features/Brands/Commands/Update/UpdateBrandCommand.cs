using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Features.Brands.Commands.Update;

public record UpdateBrandCommand : IRequest<BrandDto>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string? Description { get; init; }
    public string? LogoUrl { get; init; }
    public string? WebsiteUrl { get; init; }
    public bool IsActive { get; init; }
    public int DisplayOrder { get; init; }
}
