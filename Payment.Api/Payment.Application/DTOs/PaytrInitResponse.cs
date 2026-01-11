namespace Payment.Application.DTOs;

public class PaytrInitResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? IframeUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RawResponse { get; set; }
}

