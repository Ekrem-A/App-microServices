using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Catalog.Domain.Events;

public record ProductCreatedEvent
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public decimal Price { get; init; }
    public Guid BrandId { get; init; }
    public string BrandName { get; init; } = default!;
    public Guid? CategoryId { get; init; }
    public int StockQuantity { get; init; }
    public DateTime CreatedAt { get; init; }
}