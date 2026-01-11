namespace Payment.Application.DTOs;

public class PaytrRefundRequest
{
    public string MerchantId { get; set; } = string.Empty;
    public string MerchantOid { get; set; } = string.Empty;
    public int RefundAmount { get; set; } // in kuruş (cents)
    public string PaytrToken { get; set; } = string.Empty;
    public string? Reference { get; set; }
}

public class PaytrRefundResponse
{
    public bool Success { get; set; }
    public string? RefundId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RawResponse { get; set; }
}

