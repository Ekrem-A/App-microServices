using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Products.Commands.CreateProduct;

public record CreateProductCommand : IRequest<ProductDto>
{
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public Guid BrandId { get; init; }
    public Guid? CategoryId { get; init; }
    public decimal Price { get; init; }
    public decimal? OriginalPrice { get; init; }
    public string? Description { get; init; }
    public int StockQuantity { get; init; }
    public List<string>? Images { get; init; }
    public List<string>? Tags { get; init; }
    public bool Featured { get; init; }
    public bool IsCampaign { get; init; }
    public int DiscountPercentage { get; init; }
    public DateTimeOffset? CampaignEndDate { get; init; }
}