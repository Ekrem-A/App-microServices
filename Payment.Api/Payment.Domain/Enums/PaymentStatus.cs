namespace Payment.Domain.Enums;

public enum PaymentStatus
{
    Pending = 0,
    Processing = 1,
    Authorized = 2,
    Paid = 3,
    Failed = 4,
    Cancelled = 5,
    Refunded = 6
}

