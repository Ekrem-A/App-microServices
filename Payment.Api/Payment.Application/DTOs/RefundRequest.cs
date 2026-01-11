using System.ComponentModel.DataAnnotations;

namespace Payment.Application.DTOs;

public class RefundRequest
{
    [Required]
    public Guid PaymentId { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
    
    [MaxLength(500)]
    public string? Reason { get; set; }
}

public class RefundStatusDto
{
    public Guid RefundId { get; set; }
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? ProviderReference { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

