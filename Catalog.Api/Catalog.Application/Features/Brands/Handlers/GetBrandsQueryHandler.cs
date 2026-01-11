using AutoMapper;
using Catalog.Application.Brands.Queries.GetBrands;
using Catalog.Application.Common.Interfaces;
using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Features.Brands.Handlers;

public class GetBrandsQueryHandler : IRequestHandler<GetBrandsQuery, List<BrandDto>>
{
    private readonly IBrandQueryService _brandQueryService;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;

    public GetBrandsQueryHandler(
        IBrandQueryService brandQueryService,
        ICacheService cacheService,
        IMapper mapper)
    {
        _brandQueryService = brandQueryService;
        _cacheService = cacheService;
        _mapper = mapper;
    }

    public async Task<List<BrandDto>> Handle(GetBrandsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"brands:all:{request.IsActive}";
        var cached = await _cacheService.GetAsync<List<BrandDto>>(cacheKey, cancellationToken);

        if (cached != null)
            return cached;

        var brands = request.IsActive.HasValue && request.IsActive.Value
            ? await _brandQueryService.GetActiveBrandsAsync(cancellationToken)
            : await _brandQueryService.GetAllBrandsAsync(cancellationToken);

        var dtos = _mapper.Map<List<BrandDto>>(brands);
        await _cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(10), cancellationToken);

        return dtos;
    }
}
