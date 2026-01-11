using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Catalog.Domain.Events;

public record ProductPriceChangedEvent
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = default!;
    public decimal OldPrice { get; init; }
    public decimal NewPrice { get; init; }
    public DateTime ChangedAt { get; init; }
}