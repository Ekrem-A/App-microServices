using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Brands.Queries.GetBrands;

public record GetBrandsQuery : IRequest<List<BrandDto>>
{
    public bool? IsActive { get; init; }
}