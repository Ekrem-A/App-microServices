using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Payment.Api.Application.DTOs;
using Payment.Api.Application.Events;
using Payment.Api.Application.Interfaces;
using Payment.Api.Domain.Entities;
using Payment.Api.Domain.Enums;
using PaymentService = Payment.Api.Application.Services.PaymentService;

namespace Payment.Api.Tests.Services;

public class PaymentServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPaytrService> _paytrServiceMock;
    private readonly Mock<ILogger<PaymentService>> _loggerMock;
    private readonly PaymentService _sut;

    public PaymentServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _paytrServiceMock = new Mock<IPaytrService>();
        _loggerMock = new Mock<ILogger<PaymentService>>();

        _sut = new PaymentService(
            _unitOfWorkMock.Object,
            _paytrServiceMock.Object,
            _loggerMock.Object);
    }

    #region GetPaymentByOrderIdAsync Tests

    [Fact]
    public async Task GetPaymentByOrderIdAsync_WhenPaymentExists_ReturnsPaymentStatusDto()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var payment = CreateTestPayment(orderId);
        
        _unitOfWorkMock
            .Setup(x => x.Payments.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        // Act
        var result = await _sut.GetPaymentByOrderIdAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result!.OrderId.Should().Be(orderId);
        result.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task GetPaymentByOrderIdAsync_WhenPaymentNotExists_ReturnsNull()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        
        _unitOfWorkMock
            .Setup(x => x.Payments.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentEntity?)null);

        // Act
        var result = await _sut.GetPaymentByOrderIdAsync(orderId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region StartPaymentForOrderAsync Tests

    [Fact]
    public async Task StartPaymentForOrderAsync_WhenNewOrder_CreatesPaymentAndInitializesPayTR()
    {
        // Arrange
        var orderEvent = CreateTestOrderEvent();
        var paytrResponse = new PaytrInitResponse
        {
            Success = true,
            Token = "test-token-123",
            IframeUrl = "https://www.paytr.com/odeme/guvenli/test-token-123"
        };

        SetupMocksForNewPayment(paytrResponse);

        // Act
        var (success, iframeUrl, error) = await _sut.StartPaymentForOrderAsync(orderEvent);

        // Assert
        success.Should().BeTrue();
        iframeUrl.Should().Contain("test-token-123");
        error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Payments.AddAsync(It.IsAny<PaymentEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.PaymentAttempts.AddAsync(It.IsAny<PaymentAttemptEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartPaymentForOrderAsync_WhenPayTRFails_MarksPaymentAsFailed()
    {
        // Arrange
        var orderEvent = CreateTestOrderEvent();
        var paytrResponse = new PaytrInitResponse
        {
            Success = false,
            ErrorMessage = "Invalid merchant credentials"
        };

        SetupMocksForNewPayment(paytrResponse);

        // Act
        var (success, iframeUrl, error) = await _sut.StartPaymentForOrderAsync(orderEvent);

        // Assert
        success.Should().BeFalse();
        iframeUrl.Should().BeNull();
        error.Should().Be("Invalid merchant credentials");

        _unitOfWorkMock.Verify(x => x.OutboxMessages.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartPaymentForOrderAsync_WhenPaymentAlreadyPaid_ReturnsAlreadyProcessed()
    {
        // Arrange
        var orderEvent = CreateTestOrderEvent();
        var existingPayment = CreateTestPayment(orderEvent.OrderId);
        
        // Use reflection to set status to Paid
        typeof(PaymentEntity)
            .GetProperty("Status")!
            .SetValue(existingPayment, PaymentStatus.Paid);

        _unitOfWorkMock
            .Setup(x => x.Payments.GetByOrderIdAsync(orderEvent.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPayment);

        // Act
        var (success, iframeUrl, error) = await _sut.StartPaymentForOrderAsync(orderEvent);

        // Assert
        success.Should().BeTrue();
        error.Should().Be("Payment already processed");
        
        _paytrServiceMock.Verify(x => x.InitializePaymentAsync(It.IsAny<PaytrInitRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region HandlePaytrCallbackAsync Tests

    [Fact]
    public async Task HandlePaytrCallbackAsync_WhenSuccessCallback_MarksPaymentAsPaid()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var payment = CreateTestPayment(Guid.NewGuid());
        
        typeof(PaymentEntity)
            .GetProperty("PaymentId")!
            .SetValue(payment, paymentId);
        typeof(PaymentEntity)
            .GetProperty("Status")!
            .SetValue(payment, PaymentStatus.Processing);

        var attempt = PaymentAttemptEntity.Create(paymentId, "PayTR", "{}");

        var callback = new PaytrCallbackRequest
        {
            MerchantOid = paymentId.ToString(),
            Status = "success",
            TotalAmount = 100
        };

        _unitOfWorkMock
            .Setup(x => x.Payments.GetByIdAsync(paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        _unitOfWorkMock
            .Setup(x => x.PaymentAttempts.GetByPaymentIdAsync(paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentAttemptEntity> { attempt });

        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWorkMock.Setup(x => x.OutboxMessages.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync((OutboxMessage m, CancellationToken _) => m);
        _unitOfWorkMock.Setup(x => x.PaymentAttempts.UpdateAsync(It.IsAny<PaymentAttemptEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.Payments.UpdateAsync(It.IsAny<PaymentEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _sut.HandlePaytrCallbackAsync(callback);

        // Assert
        payment.Status.Should().Be(PaymentStatus.Paid);
        _unitOfWorkMock.Verify(x => x.OutboxMessages.AddAsync(
            It.Is<OutboxMessage>(m => m.Type == nameof(PaymentSucceededEvent)), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandlePaytrCallbackAsync_WhenFailedCallback_MarksPaymentAsFailed()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var payment = CreateTestPayment(Guid.NewGuid());
        
        typeof(PaymentEntity)
            .GetProperty("PaymentId")!
            .SetValue(payment, paymentId);
        typeof(PaymentEntity)
            .GetProperty("Status")!
            .SetValue(payment, PaymentStatus.Processing);

        var attempt = PaymentAttemptEntity.Create(paymentId, "PayTR", "{}");

        var callback = new PaytrCallbackRequest
        {
            MerchantOid = paymentId.ToString(),
            Status = "failed",
            TotalAmount = 100,
            FailedReasonCode = "INSUFFICIENT_FUNDS",
            FailedReasonMsg = "Yetersiz bakiye"
        };

        _unitOfWorkMock
            .Setup(x => x.Payments.GetByIdAsync(paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        _unitOfWorkMock
            .Setup(x => x.PaymentAttempts.GetByPaymentIdAsync(paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentAttemptEntity> { attempt });

        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWorkMock.Setup(x => x.OutboxMessages.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync((OutboxMessage m, CancellationToken _) => m);
        _unitOfWorkMock.Setup(x => x.PaymentAttempts.UpdateAsync(It.IsAny<PaymentAttemptEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.Payments.UpdateAsync(It.IsAny<PaymentEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _sut.HandlePaytrCallbackAsync(callback);

        // Assert
        payment.Status.Should().Be(PaymentStatus.Failed);
        _unitOfWorkMock.Verify(x => x.OutboxMessages.AddAsync(
            It.Is<OutboxMessage>(m => m.Type == nameof(PaymentFailedEvent)), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandlePaytrCallbackAsync_WhenInvalidMerchantOid_DoesNothing()
    {
        // Arrange
        var callback = new PaytrCallbackRequest
        {
            MerchantOid = "invalid-guid",
            Status = "success",
            TotalAmount = 100
        };

        // Act
        await _sut.HandlePaytrCallbackAsync(callback);

        // Assert
        _unitOfWorkMock.Verify(x => x.Payments.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region ProcessRefundAsync Tests

    [Fact]
    public async Task ProcessRefundAsync_WhenValidRefund_CompletesSuccessfully()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var payment = CreateTestPayment(Guid.NewGuid());
        
        typeof(PaymentEntity)
            .GetProperty("PaymentId")!
            .SetValue(payment, paymentId);
        typeof(PaymentEntity)
            .GetProperty("Status")!
            .SetValue(payment, PaymentStatus.Paid);
        typeof(PaymentEntity)
            .GetProperty("Amount")!
            .SetValue(payment, 100m);

        var refundResponse = new PaytrRefundResponse
        {
            Success = true,
            RefundId = "refund-123"
        };

        _unitOfWorkMock
            .Setup(x => x.Payments.GetByIdAsync(paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        _unitOfWorkMock
            .Setup(x => x.Refunds.GetTotalRefundedAmountAsync(paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        _paytrServiceMock
            .Setup(x => x.ProcessRefundAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refundResponse);

        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWorkMock.Setup(x => x.Refunds.AddAsync(It.IsAny<RefundEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.Refunds.UpdateAsync(It.IsAny<RefundEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.OutboxMessages.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync((OutboxMessage m, CancellationToken _) => m);

        // Act
        var (success, refund, error) = await _sut.ProcessRefundAsync(paymentId, 50m, "Customer request");

        // Assert
        success.Should().BeTrue();
        refund.Should().NotBeNull();
        refund!.Amount.Should().Be(50m);
        error.Should().BeNull();
    }

    [Fact]
    public async Task ProcessRefundAsync_WhenPaymentNotPaid_ReturnsFailed()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var payment = CreateTestPayment(Guid.NewGuid());
        
        typeof(PaymentEntity)
            .GetProperty("PaymentId")!
            .SetValue(payment, paymentId);
        typeof(PaymentEntity)
            .GetProperty("Status")!
            .SetValue(payment, PaymentStatus.Processing);

        _unitOfWorkMock
            .Setup(x => x.Payments.GetByIdAsync(paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        // Act
        var (success, refund, error) = await _sut.ProcessRefundAsync(paymentId, 50m);

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("cannot be refunded");
    }

    [Fact]
    public async Task ProcessRefundAsync_WhenAmountExceedsRefundable_ReturnsFailed()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var payment = CreateTestPayment(Guid.NewGuid());
        
        typeof(PaymentEntity)
            .GetProperty("PaymentId")!
            .SetValue(payment, paymentId);
        typeof(PaymentEntity)
            .GetProperty("Status")!
            .SetValue(payment, PaymentStatus.Paid);
        typeof(PaymentEntity)
            .GetProperty("Amount")!
            .SetValue(payment, 100m);

        _unitOfWorkMock
            .Setup(x => x.Payments.GetByIdAsync(paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        _unitOfWorkMock
            .Setup(x => x.Refunds.GetTotalRefundedAmountAsync(paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(80m); // Already refunded 80

        // Act
        var (success, refund, error) = await _sut.ProcessRefundAsync(paymentId, 50m); // Trying to refund 50 more

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("exceeds refundable amount");
    }

    #endregion

    #region Helper Methods

    private static PaymentEntity CreateTestPayment(Guid orderId)
    {
        return PaymentEntity.Create(orderId, Guid.NewGuid(), 100m, "TRY");
    }

    private static OrderCreatedEvent CreateTestOrderEvent()
    {
        return new OrderCreatedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            UserEmail = "test@example.com",
            UserName = "Test User",
            UserPhone = "5551234567",
            UserAddress = "Test Address",
            UserIp = "127.0.0.1",
            TotalAmount = 100m,
            Currency = "TRY",
            CorrelationId = Guid.NewGuid().ToString(),
            Items = new List<OrderItemDto>
            {
                new() { ProductId = Guid.NewGuid(), ProductName = "Test Product", UnitPrice = 100m, Quantity = 1 }
            }
        };
    }

    private void SetupMocksForNewPayment(PaytrInitResponse paytrResponse)
    {
        _unitOfWorkMock
            .Setup(x => x.Payments.GetByOrderIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentEntity?)null);

        _unitOfWorkMock
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _unitOfWorkMock
            .Setup(x => x.Payments.AddAsync(It.IsAny<PaymentEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentEntity p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.Payments.UpdateAsync(It.IsAny<PaymentEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.PaymentAttempts.AddAsync(It.IsAny<PaymentAttemptEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentAttemptEntity a, CancellationToken _) => a);

        _unitOfWorkMock
            .Setup(x => x.PaymentAttempts.UpdateAsync(It.IsAny<PaymentAttemptEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.OutboxMessages.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OutboxMessage m, CancellationToken _) => m);

        _paytrServiceMock
            .Setup(x => x.InitializePaymentAsync(It.IsAny<PaytrInitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paytrResponse);
    }

    #endregion
}

