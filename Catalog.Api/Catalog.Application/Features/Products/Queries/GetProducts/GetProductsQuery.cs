using Catalog.Application.Common;
using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Features.Products.Queries.GetProducts;

public record GetAllProductsQuery : IRequest<Result<List<ProductDto>>>;
