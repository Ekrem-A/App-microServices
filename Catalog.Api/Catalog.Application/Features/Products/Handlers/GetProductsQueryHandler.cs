using AutoMapper;
using Catalog.Application.Common;
using Catalog.Application.Common.Interfaces;
using Catalog.Application.DTOs;
using Catalog.Application.Features.Products.Queries.GetProducts;
using Catalog.Domain.Interfaces;
using MediatR;

public class GetAllProductsQueryHandler : IRequestHandler<GetAllProductsQuery, Result<List<ProductDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllProductsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<List<ProductDto>>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _unitOfWork.ProductQueries.GetAllProductsWithIncludesAsync(cancellationToken);
        var productDtos = _mapper.Map<List<ProductDto>>(products);

        return Result<List<ProductDto>>.Success(productDtos);
    }
}