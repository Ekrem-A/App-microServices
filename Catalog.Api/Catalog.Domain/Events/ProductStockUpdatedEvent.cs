using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Catalog.Domain.Events;

public record ProductStockUpdatedEvent
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = default!;
    public int OldStock { get; init; }
    public int NewStock { get; init; }
    public bool InStock { get; init; }
    public DateTime UpdatedAt { get; init; }
}