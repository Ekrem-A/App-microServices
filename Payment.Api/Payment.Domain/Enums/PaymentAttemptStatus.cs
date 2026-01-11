namespace Payment.Domain.Enums;

public enum PaymentAttemptStatus
{
    Initiated = 0,
    WaitingCallback = 1,
    Success = 2,
    Failed = 3,
    Expired = 4
}

