using AutoMapper;
using Catalog.Application.Common.Interfaces;
using Catalog.Application.DTOs;
using Catalog.Application.Features.Products.Queries.GetProductsById;
using MediatR;

namespace Catalog.Application.Features.Products.Handlers;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IProductQueryService _productQueryService;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;

    public GetProductByIdQueryHandler(
        IProductQueryService productQueryService,
        ICacheService cacheService,
        IMapper mapper)
    {
        _productQueryService = productQueryService;
        _cacheService = cacheService;
        _mapper = mapper;
    }

    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"products:{request.Id}";
        var cachedProduct = await _cacheService.GetAsync<ProductDto>(cacheKey, cancellationToken);

        if (cachedProduct != null)
            return cachedProduct;

        var product = await _productQueryService.GetProductByIdWithIncludesAsync(request.Id, cancellationToken);

        if (product == null)
            return null;

        var productDto = _mapper.Map<ProductDto>(product);
        await _cacheService.SetAsync(cacheKey, productDto, TimeSpan.FromMinutes(5), cancellationToken);

        return productDto;
    }
}