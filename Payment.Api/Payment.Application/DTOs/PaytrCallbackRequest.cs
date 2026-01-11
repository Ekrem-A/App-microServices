namespace Payment.Application.DTOs;

public class PaytrCallbackRequest
{
    public string MerchantOid { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string? FailedReasonCode { get; set; }
    public string? FailedReasonMsg { get; set; }
    public string? TestMode { get; set; }
    public string? PaymentType { get; set; }
    public string? Currency { get; set; }
    public int? PaymentAmount { get; set; }
    
    // Original form data for hash verification
    public string? RawData { get; set; }
}

