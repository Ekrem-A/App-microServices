namespace Payment.Infrastructure.Configuration;

public class PaytrSettings
{
    public const string SectionName = "PayTR";
    
    public string MerchantId { get; set; } = string.Empty;
    public string MerchantKey { get; set; } = string.Empty;
    public string MerchantSalt { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://www.paytr.com";
    public string SuccessUrl { get; set; } = string.Empty;
    public string FailUrl { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
    public bool TestMode { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
    public string Lang { get; set; } = "tr";
}

