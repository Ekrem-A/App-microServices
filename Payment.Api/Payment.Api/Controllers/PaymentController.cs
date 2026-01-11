using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Payment.Application.DTOs;
using Payment.Application.Services;
using Payment.Infrastructure.Configuration;

namespace Payment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly PaymentService _paymentService;
    private readonly PaytrSettings _paytrSettings;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        PaymentService paymentService,
        IOptions<PaytrSettings> paytrSettings,
        ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _paytrSettings = paytrSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Get payment status by order ID
    /// </summary>
    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(typeof(PaymentStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentByOrderId(Guid orderId, CancellationToken cancellationToken)
    {
        var payment = await _paymentService.GetPaymentByOrderIdAsync(orderId, cancellationToken);
        
        if (payment == null)
            return NotFound(new { message = $"Payment not found for order {orderId}" });

        return Ok(payment);
    }

    /// <summary>
    /// PayTR callback endpoint (Webhook)
    /// </summary>
    [HttpPost("callback")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> PaytrCallback([FromForm] PaytrCallbackFormModel form, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PayTR callback received for MerchantOid: {MerchantOid}, Status: {Status}",
            form.MerchantOid, form.Status);

        var callback = new PaytrCallbackRequest
        {
            MerchantOid = form.MerchantOid ?? string.Empty,
            Status = form.Status ?? string.Empty,
            TotalAmount = form.TotalAmount,
            Hash = form.Hash ?? string.Empty,
            FailedReasonCode = form.FailedReasonCode,
            FailedReasonMsg = form.FailedReasonMsg,
            TestMode = form.TestMode,
            PaymentType = form.PaymentType,
            Currency = form.Currency,
            PaymentAmount = form.PaymentAmount
        };

        // Verify hash (important for security)
        var expectedHash = GenerateCallbackHash(callback);
        if (!callback.Hash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Invalid hash for callback. MerchantOid: {MerchantOid}, Expected: {Expected}, Received: {Received}",
                callback.MerchantOid, expectedHash, callback.Hash);
            
            // PayTR expects "OK" response even for invalid callbacks to stop retrying
            return Content("OK");
        }

        try
        {
            await _paymentService.HandlePaytrCallbackAsync(callback, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayTR callback for MerchantOid: {MerchantOid}", callback.MerchantOid);
            // Don't return error to PayTR, handle internally
        }

        // PayTR requires "OK" response
        return Content("OK");
    }

    private string GenerateCallbackHash(PaytrCallbackRequest callback)
    {
        // Hash = base64(hmac_sha256(merchant_key, merchant_oid + merchant_salt + status + total_amount))
        var hashStr = string.Concat(
            callback.MerchantOid,
            _paytrSettings.MerchantSalt,
            callback.Status,
            callback.TotalAmount.ToString("F0")
        );

        using var hmac = new System.Security.Cryptography.HMACSHA256(
            System.Text.Encoding.UTF8.GetBytes(_paytrSettings.MerchantKey));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(hashStr));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Process a refund for a payment
    /// </summary>
    [HttpPost("refund")]
    [ProducesResponseType(typeof(RefundStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessRefund([FromBody] RefundRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, refund, error) = await _paymentService.ProcessRefundAsync(
            request.PaymentId, 
            request.Amount, 
            request.Reason, 
            cancellationToken);

        if (!success)
        {
            if (error?.Contains("not found") == true)
                return NotFound(new { message = error });
            
            return BadRequest(new { message = error, refund });
        }

        return Ok(refund);
    }

    /// <summary>
    /// Get refunds for a payment
    /// </summary>
    [HttpGet("{paymentId:guid}/refunds")]
    [ProducesResponseType(typeof(IEnumerable<RefundStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRefundsByPaymentId(Guid paymentId, CancellationToken cancellationToken)
    {
        var refunds = await _paymentService.GetRefundsByPaymentIdAsync(paymentId, cancellationToken);
        return Ok(refunds);
    }

    /// <summary>
    /// Get refund by ID
    /// </summary>
    [HttpGet("refund/{refundId:guid}")]
    [ProducesResponseType(typeof(RefundStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRefundById(Guid refundId, CancellationToken cancellationToken)
    {
        var refund = await _paymentService.GetRefundByIdAsync(refundId, cancellationToken);
        
        if (refund == null)
            return NotFound(new { message = $"Refund not found: {refundId}" });

        return Ok(refund);
    }
}

/// <summary>
/// Form model for PayTR callback (form-urlencoded)
/// </summary>
public class PaytrCallbackFormModel
{
    [FromForm(Name = "merchant_oid")]
    public string? MerchantOid { get; set; }

    [FromForm(Name = "status")]
    public string? Status { get; set; }

    [FromForm(Name = "total_amount")]
    public decimal TotalAmount { get; set; }

    [FromForm(Name = "hash")]
    public string? Hash { get; set; }

    [FromForm(Name = "failed_reason_code")]
    public string? FailedReasonCode { get; set; }

    [FromForm(Name = "failed_reason_msg")]
    public string? FailedReasonMsg { get; set; }

    [FromForm(Name = "test_mode")]
    public string? TestMode { get; set; }

    [FromForm(Name = "payment_type")]
    public string? PaymentType { get; set; }

    [FromForm(Name = "currency")]
    public string? Currency { get; set; }

    [FromForm(Name = "payment_amount")]
    public int? PaymentAmount { get; set; }
}

