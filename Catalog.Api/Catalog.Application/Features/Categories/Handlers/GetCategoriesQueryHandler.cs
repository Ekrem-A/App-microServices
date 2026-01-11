using AutoMapper;
using Catalog.Application.Features.Categories.Queries.GetCategories;
using Catalog.Application.Common.Interfaces;
using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Features.Categories.Handlers;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    private readonly ICategoryQueryService _categoryQueryService;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;

    public GetCategoriesQueryHandler(
        ICategoryQueryService categoryQueryService,
        ICacheService cacheService,
        IMapper mapper)
    {
        _categoryQueryService = categoryQueryService;
        _cacheService = cacheService;
        _mapper = mapper;
    }

    public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"categories:all:{request.IsActive}:{request.ParentId}";
        var cached = await _cacheService.GetAsync<List<CategoryDto>>(cacheKey, cancellationToken);

        if (cached != null)
            return cached;

        var categories = await _categoryQueryService.GetCategoriesWithFilteringAsync(
            request.IsActive,
            request.ParentId,
            cancellationToken);

        var dtos = _mapper.Map<List<CategoryDto>>(categories);
        await _cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(10), cancellationToken);

        return dtos;
    }
}