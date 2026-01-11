using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Payment.Api.Application.DTOs;
using Payment.Api.Controllers;
using Payment.Api.Domain.Enums;
using Payment.Api.Infrastructure.Configuration;
using PaymentService = Payment.Api.Application.Services.PaymentService;

namespace Payment.Api.Tests.Controllers;

public class PaymentControllerTests
{
    private readonly Mock<PaymentService> _paymentServiceMock;
    private readonly Mock<IOptions<PaytrSettings>> _paytrSettingsMock;
    private readonly Mock<ILogger<PaymentController>> _loggerMock;
    private readonly PaymentController _sut;

    public PaymentControllerTests()
    {
        _paymentServiceMock = new Mock<PaymentService>(
            MockBehavior.Strict,
            null!, null!, null!);
        
        _paytrSettingsMock = new Mock<IOptions<PaytrSettings>>();
        _paytrSettingsMock.Setup(x => x.Value).Returns(new PaytrSettings
        {
            MerchantId = "test-merchant",
            MerchantKey = "test-key",
            MerchantSalt = "test-salt"
        });
        
        _loggerMock = new Mock<ILogger<PaymentController>>();

        _sut = new PaymentController(
            _paymentServiceMock.Object,
            _paytrSettingsMock.Object,
            _loggerMock.Object);
    }

    #region GetPaymentByOrderId Tests

    [Fact]
    public async Task GetPaymentByOrderId_WhenPaymentExists_ReturnsOk()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var paymentDto = new PaymentStatusDto
        {
            PaymentId = Guid.NewGuid(),
            OrderId = orderId,
            Amount = 100m,
            Currency = "TRY",
            Status = PaymentStatus.Paid,
            CreatedAt = DateTime.UtcNow
        };

        _paymentServiceMock
            .Setup(x => x.GetPaymentByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paymentDto);

        // Act
        var result = await _sut.GetPaymentByOrderId(orderId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedPayment = okResult.Value.Should().BeOfType<PaymentStatusDto>().Subject;
        returnedPayment.OrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task GetPaymentByOrderId_WhenPaymentNotExists_ReturnsNotFound()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _paymentServiceMock
            .Setup(x => x.GetPaymentByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentStatusDto?)null);

        // Act
        var result = await _sut.GetPaymentByOrderId(orderId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region ProcessRefund Tests

    [Fact]
    public async Task ProcessRefund_WhenSuccessful_ReturnsOk()
    {
        // Arrange
        var request = new RefundRequest
        {
            PaymentId = Guid.NewGuid(),
            Amount = 50m,
            Reason = "Customer request"
        };

        var refundDto = new RefundStatusDto
        {
            RefundId = Guid.NewGuid(),
            PaymentId = request.PaymentId,
            Amount = request.Amount,
            Status = RefundStatus.Completed.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        _paymentServiceMock
            .Setup(x => x.ProcessRefundAsync(request.PaymentId, request.Amount, request.Reason, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, refundDto, (string?)null));

        // Act
        var result = await _sut.ProcessRefund(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRefund = okResult.Value.Should().BeOfType<RefundStatusDto>().Subject;
        returnedRefund.Amount.Should().Be(50m);
    }

    [Fact]
    public async Task ProcessRefund_WhenPaymentNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new RefundRequest
        {
            PaymentId = Guid.NewGuid(),
            Amount = 50m
        };

        _paymentServiceMock
            .Setup(x => x.ProcessRefundAsync(request.PaymentId, request.Amount, request.Reason, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, (RefundStatusDto?)null, "Payment not found"));

        // Act
        var result = await _sut.ProcessRefund(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ProcessRefund_WhenRefundFails_ReturnsBadRequest()
    {
        // Arrange
        var request = new RefundRequest
        {
            PaymentId = Guid.NewGuid(),
            Amount = 50m
        };

        var refundDto = new RefundStatusDto
        {
            RefundId = Guid.NewGuid(),
            PaymentId = request.PaymentId,
            Amount = request.Amount,
            Status = RefundStatus.Failed.ToString()
        };

        _paymentServiceMock
            .Setup(x => x.ProcessRefundAsync(request.PaymentId, request.Amount, request.Reason, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, refundDto, "Refund processing failed"));

        // Act
        var result = await _sut.ProcessRefund(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetRefundsByPaymentId Tests

    [Fact]
    public async Task GetRefundsByPaymentId_ReturnsRefundsList()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var refunds = new List<RefundStatusDto>
        {
            new() { RefundId = Guid.NewGuid(), PaymentId = paymentId, Amount = 25m },
            new() { RefundId = Guid.NewGuid(), PaymentId = paymentId, Amount = 50m }
        };

        _paymentServiceMock
            .Setup(x => x.GetRefundsByPaymentIdAsync(paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refunds);

        // Act
        var result = await _sut.GetRefundsByPaymentId(paymentId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRefunds = okResult.Value.Should().BeAssignableTo<IEnumerable<RefundStatusDto>>().Subject;
        returnedRefunds.Should().HaveCount(2);
    }

    #endregion

    #region GetRefundById Tests

    [Fact]
    public async Task GetRefundById_WhenRefundExists_ReturnsOk()
    {
        // Arrange
        var refundId = Guid.NewGuid();
        var refundDto = new RefundStatusDto
        {
            RefundId = refundId,
            PaymentId = Guid.NewGuid(),
            Amount = 50m,
            Status = RefundStatus.Completed.ToString()
        };

        _paymentServiceMock
            .Setup(x => x.GetRefundByIdAsync(refundId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refundDto);

        // Act
        var result = await _sut.GetRefundById(refundId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRefund = okResult.Value.Should().BeOfType<RefundStatusDto>().Subject;
        returnedRefund.RefundId.Should().Be(refundId);
    }

    [Fact]
    public async Task GetRefundById_WhenRefundNotExists_ReturnsNotFound()
    {
        // Arrange
        var refundId = Guid.NewGuid();

        _paymentServiceMock
            .Setup(x => x.GetRefundByIdAsync(refundId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefundStatusDto?)null);

        // Act
        var result = await _sut.GetRefundById(refundId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion
}

