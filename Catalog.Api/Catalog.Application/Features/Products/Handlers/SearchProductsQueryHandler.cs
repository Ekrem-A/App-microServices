using Catalog.Application.Common;
using Catalog.Application.Common.Interfaces;
using Catalog.Application.DTOs;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Catalog.Application.Features.Products.Queries.SearchProducts;

namespace Catalog.Application.Features.Products.Handlers;

public class SearchProductsQueryHandler : IRequestHandler<SearchProductsQuery, Result<List<ProductDto>>>
{
    private readonly IProductQueryService _productQueryService;
    private readonly IMapper _mapper;
    private readonly ILogger<SearchProductsQueryHandler> _logger;

    public SearchProductsQueryHandler(
        IProductQueryService productQueryService,
        IMapper mapper,
        ILogger<SearchProductsQueryHandler> logger)
    {
        _productQueryService = productQueryService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<List<ProductDto>>> Handle(
        SearchProductsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                return Result<List<ProductDto>>.Failure("Arama terimi boş olamaz.");
            }

            var searchResult = await _productQueryService
                .SearchProductsAsync(request.SearchTerm, 1, 100, null, null, null, null, cancellationToken);
            var products = searchResult.Items;

                var productDtos = _mapper.Map<List<ProductDto>>(products);

                _logger.LogInformation(
                    "Arama terimi '{SearchTerm}' için {Count} ürün bulundu",
                    request.SearchTerm,
                    productDtos.Count);

                return Result<List<ProductDto>>.Success(productDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün arama sırasında hata oluştu");
                return Result<List<ProductDto>>.Failure("Ürün arama sırasında bir hata oluştu.");
            }
        }
    };