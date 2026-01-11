namespace Order.Domain.Common;

public abstract class AggregateRoot : Entity
{
    public byte[] RowVersion { get; protected set; } = Array.Empty<byte>();

    protected AggregateRoot() : base()
    {
    }

    protected AggregateRoot(Guid id) : base(id)
    {
    }
}

