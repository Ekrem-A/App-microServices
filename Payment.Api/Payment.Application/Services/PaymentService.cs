using System.Text.Json;
using Microsoft.Extensions.Logging;
using Payment.Application.DTOs;
using Payment.Application.Events;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.Enums;

namespace Payment.Application.Services;

public class PaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaytrService _paytrService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IUnitOfWork unitOfWork, 
        IPaytrService paytrService,
        ILogger<PaymentService> logger)
    {
        _unitOfWork = unitOfWork;
        _paytrService = paytrService;
        _logger = logger;
    }

    public async Task<PaymentStatusDto?> GetPaymentByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var payment = await _unitOfWork.Payments.GetByOrderIdAsync(orderId, cancellationToken);
        if (payment == null) return null;

        return new PaymentStatusDto
        {
            PaymentId = payment.PaymentId,
            OrderId = payment.OrderId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Status = payment.Status,
            ProviderReference = payment.ProviderReference,
            FailureReason = payment.FailureReason,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };
    }

    public async Task<(bool Success, string? IframeUrl, string? Error)> StartPaymentForOrderAsync(
        OrderCreatedEvent orderEvent, 
        CancellationToken cancellationToken = default)
    {
        // Idempotency check: if payment already exists for this order, return existing status
        var existingPayment = await _unitOfWork.Payments.GetByOrderIdAsync(orderEvent.OrderId, cancellationToken);
        if (existingPayment != null)
        {
            _logger.LogInformation("Payment already exists for OrderId {OrderId}, PaymentId {PaymentId}, Status {Status}", 
                orderEvent.OrderId, existingPayment.PaymentId, existingPayment.Status);
            
            if (existingPayment.IsFinalState())
            {
                return (existingPayment.Status == PaymentStatus.Paid, null, "Payment already processed");
            }
            
            // Return existing iframe URL if available from latest attempt
            var attempts = await _unitOfWork.PaymentAttempts.GetByPaymentIdAsync(existingPayment.PaymentId, cancellationToken);
            var latestAttempt = attempts.OrderByDescending(a => a.CreatedAt).FirstOrDefault();
            if (latestAttempt?.Status == PaymentAttemptStatus.WaitingCallback && latestAttempt.ProviderReference != null)
            {
                return (true, $"https://www.paytr.com/odeme/guvenli/{latestAttempt.ProviderReference}", null);
            }
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // Create payment entity
            var payment = existingPayment ?? PaymentEntity.Create(
                orderEvent.OrderId,
                orderEvent.UserId,
                orderEvent.TotalAmount,
                orderEvent.Currency);

            if (existingPayment == null)
            {
                await _unitOfWork.Payments.AddAsync(payment, cancellationToken);
            }
            
            payment.MarkAsProcessing();

            // Prepare PayTR request
            var paytrRequest = new PaytrInitRequest
            {
                OrderId = orderEvent.OrderId,
                UserId = orderEvent.UserId,
                UserEmail = orderEvent.UserEmail,
                UserName = orderEvent.UserName,
                UserPhone = orderEvent.UserPhone,
                UserAddress = orderEvent.UserAddress,
                UserIp = orderEvent.UserIp,
                TotalAmount = orderEvent.TotalAmount,
                Currency = MapCurrency(orderEvent.Currency),
                MerchantOid = payment.PaymentId.ToString(),
                BasketItems = orderEvent.Items.Select(i => new PaytrBasketItem
                {
                    Name = i.ProductName,
                    Price = i.UnitPrice,
                    Quantity = i.Quantity
                }).ToList()
            };

            // Create payment attempt
            var attempt = PaymentAttemptEntity.Create(
                payment.PaymentId, 
                "PayTR",
                JsonSerializer.Serialize(paytrRequest));
            
            await _unitOfWork.PaymentAttempts.AddAsync(attempt, cancellationToken);

            // Call PayTR API
            var paytrResponse = await _paytrService.InitializePaymentAsync(paytrRequest, cancellationToken);

            if (paytrResponse.Success && !string.IsNullOrEmpty(paytrResponse.Token))
            {
                attempt.MarkAsWaitingCallback(paytrResponse.Token, paytrResponse.RawResponse);
                await _unitOfWork.PaymentAttempts.UpdateAsync(attempt, cancellationToken);
                await _unitOfWork.Payments.UpdateAsync(payment, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Payment initiated for OrderId {OrderId}, PaymentId {PaymentId}, Token {Token}",
                    orderEvent.OrderId, payment.PaymentId, paytrResponse.Token);

                return (true, paytrResponse.IframeUrl, null);
            }
            else
            {
                attempt.MarkAsFailed(paytrResponse.ErrorMessage ?? "Unknown error", paytrResponse.RawResponse);
                payment.MarkAsFailed(paytrResponse.ErrorMessage ?? "PayTR initialization failed");

                // Add failure event to outbox
                var failedEvent = new PaymentFailedEvent
                {
                    OrderId = orderEvent.OrderId,
                    PaymentId = payment.PaymentId,
                    ReasonCode = "INIT_FAILED",
                    ReasonMessage = paytrResponse.ErrorMessage ?? "Payment initialization failed",
                    FailedAt = DateTime.UtcNow,
                    CorrelationId = orderEvent.CorrelationId
                };

                var outboxMessage = OutboxMessage.Create(
                    nameof(PaymentFailedEvent),
                    JsonSerializer.Serialize(failedEvent),
                    orderEvent.CorrelationId);

                await _unitOfWork.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
                await _unitOfWork.PaymentAttempts.UpdateAsync(attempt, cancellationToken);
                await _unitOfWork.Payments.UpdateAsync(payment, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogWarning("Payment initialization failed for OrderId {OrderId}: {Error}", 
                    orderEvent.OrderId, paytrResponse.ErrorMessage);

                return (false, null, paytrResponse.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error starting payment for OrderId {OrderId}", orderEvent.OrderId);
            throw;
        }
    }

    public async Task HandlePaytrCallbackAsync(PaytrCallbackRequest callback, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received PayTR callback for MerchantOid {MerchantOid}, Status {Status}",
            callback.MerchantOid, callback.Status);

        // MerchantOid is our PaymentId
        if (!Guid.TryParse(callback.MerchantOid, out var paymentId))
        {
            _logger.LogWarning("Invalid MerchantOid format: {MerchantOid}", callback.MerchantOid);
            return;
        }

        var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId, cancellationToken);
        if (payment == null)
        {
            _logger.LogWarning("Payment not found for PaymentId {PaymentId}", paymentId);
            return;
        }

        // Idempotency: skip if already in final state
        if (payment.IsFinalState())
        {
            _logger.LogInformation("Payment {PaymentId} already in final state {Status}, skipping callback",
                paymentId, payment.Status);
            return;
        }

        var attempts = await _unitOfWork.PaymentAttempts.GetByPaymentIdAsync(paymentId, cancellationToken);
        var latestAttempt = attempts.OrderByDescending(a => a.CreatedAt).FirstOrDefault();

        if (latestAttempt == null)
        {
            _logger.LogWarning("No attempt found for PaymentId {PaymentId}", paymentId);
            return;
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            if (callback.Status == "success")
            {
                latestAttempt.MarkAsSuccess(JsonSerializer.Serialize(callback));
                payment.MarkAsPaid(latestAttempt.ProviderReference ?? paymentId.ToString());

                var successEvent = new PaymentSucceededEvent
                {
                    OrderId = payment.OrderId,
                    PaymentId = payment.PaymentId,
                    ProviderReference = latestAttempt.ProviderReference ?? paymentId.ToString(),
                    PaidAmount = payment.Amount,
                    Currency = payment.Currency,
                    PaidAt = DateTime.UtcNow
                };

                var outboxMessage = OutboxMessage.Create(
                    nameof(PaymentSucceededEvent),
                    JsonSerializer.Serialize(successEvent));

                await _unitOfWork.OutboxMessages.AddAsync(outboxMessage, cancellationToken);

                _logger.LogInformation("Payment succeeded for PaymentId {PaymentId}, OrderId {OrderId}",
                    paymentId, payment.OrderId);
            }
            else
            {
                var errorMessage = callback.FailedReasonMsg ?? callback.FailedReasonCode ?? "Payment failed";
                latestAttempt.MarkAsFailed(errorMessage, JsonSerializer.Serialize(callback));
                payment.MarkAsFailed(errorMessage);

                var failedEvent = new PaymentFailedEvent
                {
                    OrderId = payment.OrderId,
                    PaymentId = payment.PaymentId,
                    ReasonCode = callback.FailedReasonCode ?? "UNKNOWN",
                    ReasonMessage = errorMessage,
                    FailedAt = DateTime.UtcNow
                };

                var outboxMessage = OutboxMessage.Create(
                    nameof(PaymentFailedEvent),
                    JsonSerializer.Serialize(failedEvent));

                await _unitOfWork.OutboxMessages.AddAsync(outboxMessage, cancellationToken);

                _logger.LogWarning("Payment failed for PaymentId {PaymentId}, OrderId {OrderId}: {Error}",
                    paymentId, payment.OrderId, errorMessage);
            }

            await _unitOfWork.PaymentAttempts.UpdateAsync(latestAttempt, cancellationToken);
            await _unitOfWork.Payments.UpdateAsync(payment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error handling PayTR callback for PaymentId {PaymentId}", paymentId);
            throw;
        }
    }

    public async Task<(bool Success, RefundStatusDto? Refund, string? Error)> ProcessRefundAsync(
        Guid paymentId, 
        decimal amount, 
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId, cancellationToken);
        if (payment == null)
        {
            return (false, null, "Payment not found");
        }

        // Only paid payments can be refunded
        if (payment.Status != PaymentStatus.Paid)
        {
            return (false, null, $"Payment cannot be refunded. Current status: {payment.Status}");
        }

        // Check refundable amount
        var totalRefunded = await _unitOfWork.Refunds.GetTotalRefundedAmountAsync(paymentId, cancellationToken);
        var refundableAmount = payment.Amount - totalRefunded;

        if (amount > refundableAmount)
        {
            return (false, null, $"Refund amount ({amount}) exceeds refundable amount ({refundableAmount})");
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // Create refund entity
            var refund = RefundEntity.Create(paymentId, amount, payment.Currency, reason);
            refund.MarkAsProcessing();
            await _unitOfWork.Refunds.AddAsync(refund, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Call PayTR refund API
            var merchantOid = payment.PaymentId.ToString();
            var paytrResponse = await _paytrService.ProcessRefundAsync(
                merchantOid, 
                amount, 
                refund.RefundId.ToString(), 
                cancellationToken);

            if (paytrResponse.Success)
            {
                refund.MarkAsCompleted(paytrResponse.RefundId ?? refund.RefundId.ToString());
                
                // Check if fully refunded
                var newTotalRefunded = totalRefunded + amount;
                if (newTotalRefunded >= payment.Amount)
                {
                    payment.MarkAsRefunded();
                    await _unitOfWork.Payments.UpdateAsync(payment, cancellationToken);
                }

                // Add success event to outbox
                var refundEvent = new RefundCompletedEvent
                {
                    OrderId = payment.OrderId,
                    PaymentId = paymentId,
                    RefundId = refund.RefundId,
                    RefundAmount = amount,
                    Currency = payment.Currency,
                    Reason = reason,
                    RefundedAt = DateTime.UtcNow
                };

                var outboxMessage = OutboxMessage.Create(
                    nameof(RefundCompletedEvent),
                    JsonSerializer.Serialize(refundEvent));

                await _unitOfWork.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
                await _unitOfWork.Refunds.UpdateAsync(refund, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Refund completed for PaymentId {PaymentId}, RefundId {RefundId}, Amount {Amount}",
                    paymentId, refund.RefundId, amount);

                return (true, MapToRefundDto(refund), null);
            }
            else
            {
                refund.MarkAsFailed(paytrResponse.ErrorMessage ?? "Refund failed");

                // Add failure event to outbox
                var failedEvent = new RefundFailedEvent
                {
                    OrderId = payment.OrderId,
                    PaymentId = paymentId,
                    RefundId = refund.RefundId,
                    RefundAmount = amount,
                    ReasonCode = "REFUND_FAILED",
                    ReasonMessage = paytrResponse.ErrorMessage ?? "Refund processing failed",
                    FailedAt = DateTime.UtcNow
                };

                var outboxMessage = OutboxMessage.Create(
                    nameof(RefundFailedEvent),
                    JsonSerializer.Serialize(failedEvent));

                await _unitOfWork.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
                await _unitOfWork.Refunds.UpdateAsync(refund, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogWarning("Refund failed for PaymentId {PaymentId}: {Error}", 
                    paymentId, paytrResponse.ErrorMessage);

                return (false, MapToRefundDto(refund), paytrResponse.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error processing refund for PaymentId {PaymentId}", paymentId);
            throw;
        }
    }

    public async Task<IEnumerable<RefundStatusDto>> GetRefundsByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var refunds = await _unitOfWork.Refunds.GetByPaymentIdAsync(paymentId, cancellationToken);
        return refunds.Select(MapToRefundDto);
    }

    public async Task<RefundStatusDto?> GetRefundByIdAsync(Guid refundId, CancellationToken cancellationToken = default)
    {
        var refund = await _unitOfWork.Refunds.GetByIdAsync(refundId, cancellationToken);
        return refund == null ? null : MapToRefundDto(refund);
    }

    private static RefundStatusDto MapToRefundDto(RefundEntity refund)
    {
        return new RefundStatusDto
        {
            RefundId = refund.RefundId,
            PaymentId = refund.PaymentId,
            Amount = refund.Amount,
            Currency = refund.Currency,
            Status = refund.Status.ToString(),
            Reason = refund.Reason,
            ProviderReference = refund.ProviderReference,
            FailureReason = refund.FailureReason,
            CreatedAt = refund.CreatedAt,
            CompletedAt = refund.CompletedAt
        };
    }

    private static string MapCurrency(string currency)
    {
        return currency.ToUpperInvariant() switch
        {
            "TRY" => "TL",
            "TL" => "TL",
            "USD" => "USD",
            "EUR" => "EUR",
            "GBP" => "GBP",
            _ => "TL"
        };
    }
}

