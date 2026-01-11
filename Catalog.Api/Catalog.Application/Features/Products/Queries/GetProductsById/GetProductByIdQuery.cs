using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Features.Products.Queries.GetProductsById;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>;