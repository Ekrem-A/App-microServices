using AutoMapper;
using Catalog.Application.Common;
using Catalog.Application.Common.Interfaces;
using Catalog.Application.DTOs;
using Catalog.Application.Features.Products.Queries.GetProducts;
using MediatR;

namespace Catalog.Application.Features.Products.Handlers;

public class GetAllProductsQueryHandler : IRequestHandler<GetAllProductsQuery, Result<List<ProductDto>>>
{
    private readonly IProductQueryService _productQueryService;
    private readonly IMapper _mapper;

    public GetAllProductsQueryHandler(IProductQueryService productQueryService, IMapper mapper)
    {
        _productQueryService = productQueryService;
        _mapper = mapper;
    }

    public async Task<Result<List<ProductDto>>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _productQueryService.GetAllProductsWithIncludesAsync(cancellationToken);
        var productDtos = _mapper.Map<List<ProductDto>>(products);

        return Result<List<ProductDto>>.Success(productDtos);
    }
}
