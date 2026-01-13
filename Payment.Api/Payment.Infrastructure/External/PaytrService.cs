using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Infrastructure.Configuration;

namespace Payment.Infrastructure.External;

public class PaytrService : IPaytrService
{
    private readonly HttpClient _httpClient;
    private readonly PaytrSettings _settings;
    private readonly ILogger<PaytrService> _logger;

    // Cached options and encoder to avoid repeated allocations
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PaytrService(
        HttpClient httpClient,
        IOptions<PaytrSettings> settings,
        ILogger<PaytrService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<PaytrInitResponse> InitializePaymentAsync(PaytrInitRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Format basket for PayTR (base64 encoded JSON array)
            var basketJson = JsonSerializer.Serialize(request.BasketItems.Select(item => new object[]
            {
                item.Name,
                FormatAmount(item.Price),
                item.Quantity
            }));
            var basketBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(basketJson));

            // PayTR expects amount in kuruş (cents)
            var paymentAmountKurus = (int)(request.TotalAmount * 100);

            // Generate merchant_oid (unique order id for PayTR)
            var merchantOid = request.MerchantOid;

            // Calculate PayTR token hash using StringBuilder for better performance
            var hashBuilder = new StringBuilder(256);
            hashBuilder.Append(_settings.MerchantId);
            hashBuilder.Append(request.UserIp);
            hashBuilder.Append(merchantOid);
            hashBuilder.Append(request.UserEmail);
            hashBuilder.Append(paymentAmountKurus);
            hashBuilder.Append(basketBase64);
            hashBuilder.Append(request.NoInstallment ? '1' : '0');
            hashBuilder.Append(request.MaxInstallment);
            hashBuilder.Append(request.Currency);
            hashBuilder.Append(_settings.TestMode ? '1' : '0');
            hashBuilder.Append(_settings.MerchantSalt);

            var paytrToken = GenerateHash(hashBuilder.ToString());

            // Build form data
            var formData = new Dictionary<string, string>(16)
            {
                { "merchant_id", _settings.MerchantId },
                { "user_ip", request.UserIp },
                { "merchant_oid", merchantOid },
                { "email", request.UserEmail },
                { "payment_amount", paymentAmountKurus.ToString() },
                { "paytr_token", paytrToken },
                { "user_basket", basketBase64 },
                { "debug_on", _settings.TestMode ? "1" : "0" },
                { "no_installment", request.NoInstallment ? "1" : "0" },
                { "max_installment", request.MaxInstallment.ToString() },
                { "user_name", request.UserName },
                { "user_address", request.UserAddress },
                { "user_phone", request.UserPhone },
                { "merchant_ok_url", _settings.SuccessUrl },
                { "merchant_fail_url", _settings.FailUrl },
                { "timeout_limit", _settings.TimeoutSeconds.ToString() },
                { "currency", request.Currency },
                { "test_mode", _settings.TestMode ? "1" : "0" },
                { "lang", _settings.Lang }
            };

            _logger.LogDebug("Sending PayTR init request for MerchantOid {MerchantOid}", merchantOid);

            using var content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.PostAsync("/odeme/api/get-token", content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("PayTR response: {Response}", responseBody);

            var result = JsonSerializer.Deserialize<PaytrApiResponse>(responseBody, s_jsonOptions);

            if (result?.Status == "success" && !string.IsNullOrEmpty(result.Token))
            {
                return new PaytrInitResponse
                {
                    Success = true,
                    Token = result.Token,
                    IframeUrl = $"{_settings.BaseUrl}/odeme/guvenli/{result.Token}",
                    RawResponse = responseBody
                };
            }

            return new PaytrInitResponse
            {
                Success = false,
                ErrorMessage = result?.Reason ?? "Unknown error from PayTR",
                RawResponse = responseBody
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing PayTR payment");
            return new PaytrInitResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<PaytrRefundResponse> ProcessRefundAsync(string merchantOid, decimal amount, string? reference = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // PayTR expects amount in kuruş (cents)
            var refundAmountKurus = (int)(amount * 100);
            
            // Generate refund hash using StringBuilder
            var hashBuilder = new StringBuilder(128);
            hashBuilder.Append(_settings.MerchantId);
            hashBuilder.Append(merchantOid);
            hashBuilder.Append(refundAmountKurus);
            hashBuilder.Append(_settings.MerchantSalt);
            
            var paytrToken = GenerateHash(hashBuilder.ToString());

            var formData = new Dictionary<string, string>(5)
            {
                { "merchant_id", _settings.MerchantId },
                { "merchant_oid", merchantOid },
                { "return_amount", refundAmountKurus.ToString() },
                { "paytr_token", paytrToken }
            };

            if (!string.IsNullOrEmpty(reference))
            {
                formData.Add("reference_no", reference);
            }

            _logger.LogDebug("Sending PayTR refund request for MerchantOid {MerchantOid}, Amount {Amount}", 
                merchantOid, amount);

            using var content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.PostAsync("/odeme/iade", content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("PayTR refund response: {Response}", responseBody);

            var result = JsonSerializer.Deserialize<PaytrRefundApiResponse>(responseBody, s_jsonOptions);

            if (result?.Status == "success")
            {
                return new PaytrRefundResponse
                {
                    Success = true,
                    RefundId = result.RefundId ?? merchantOid,
                    RawResponse = responseBody
                };
            }

            return new PaytrRefundResponse
            {
                Success = false,
                ErrorMessage = result?.ErrMsg ?? "Unknown error from PayTR",
                RawResponse = responseBody
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayTR refund for MerchantOid {MerchantOid}", merchantOid);
            return new PaytrRefundResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public bool VerifyCallback(PaytrCallbackRequest callback, string expectedHash)
    {
        var calculatedHash = GenerateCallbackHash(callback);
        return calculatedHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    public string GenerateCallbackHash(PaytrCallbackRequest callback)
    {
        // PayTR callback hash: merchant_oid + merchant_salt + status + total_amount
        var hashBuilder = new StringBuilder(128);
        hashBuilder.Append(callback.MerchantOid);
        hashBuilder.Append(_settings.MerchantSalt);
        hashBuilder.Append(callback.Status);
        hashBuilder.Append(callback.TotalAmount.ToString("F0"));
        
        return GenerateHash(hashBuilder.ToString());
    }

    private string GenerateHash(string input)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_settings.MerchantKey);
        var inputBytes = Encoding.UTF8.GetBytes(input);
        
        // Use stackalloc for small buffers to avoid heap allocation
        Span<byte> hashBuffer = stackalloc byte[32]; // SHA256 produces 32 bytes
        
        HMACSHA256.HashData(keyBytes, inputBytes, hashBuffer);
        return Convert.ToBase64String(hashBuffer);
    }

    private static string FormatAmount(decimal amount)
    {
        return ((int)(amount * 100)).ToString();
    }

    private sealed class PaytrApiResponse
    {
        public string? Status { get; set; }
        public string? Token { get; set; }
        public string? Reason { get; set; }
    }

    private sealed class PaytrRefundApiResponse
    {
        public string? Status { get; set; }
        public string? RefundId { get; set; }
        public string? ErrMsg { get; set; }
    }
}

