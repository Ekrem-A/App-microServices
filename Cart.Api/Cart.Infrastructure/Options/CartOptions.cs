namespace Cart.Infrastructure.Options;

public class CartOptions
{
    public const string SectionName = "Cart";

    public int TtlDays { get; set; } = 30;
}

