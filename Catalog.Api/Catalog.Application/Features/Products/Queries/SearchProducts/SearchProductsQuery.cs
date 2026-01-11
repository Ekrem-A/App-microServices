using Catalog.Application.Common;
using Catalog.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Catalog.Application.Features.Products.Queries.SearchProducts
{
    public record SearchProductsQuery(string SearchTerm) : IRequest<Result<List<ProductDto>>>;

}