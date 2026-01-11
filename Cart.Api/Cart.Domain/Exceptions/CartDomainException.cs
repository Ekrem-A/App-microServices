namespace Cart.Domain.Exceptions;

public class CartDomainException : Exception
{
    public CartDomainException(string message)
        : base(message)
    {
    }

    public CartDomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

