using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Catalog.Domain.Events;

public record ProductDeletedEvent
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = default!;
    public DateTime DeletedAt { get; init; }
}