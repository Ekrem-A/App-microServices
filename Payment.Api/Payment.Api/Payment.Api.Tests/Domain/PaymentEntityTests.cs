using FluentAssertions;
using Payment.Api.Domain.Entities;
using Payment.Api.Domain.Enums;

namespace Payment.Api.Tests.Domain;

public class PaymentEntityTests
{
    [Fact]
    public void Create_ShouldCreatePaymentWithCorrectValues()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var amount = 150.50m;
        var currency = "TRY";

        // Act
        var payment = PaymentEntity.Create(orderId, userId, amount, currency);

        // Assert
        payment.Should().NotBeNull();
        payment.PaymentId.Should().NotBeEmpty();
        payment.OrderId.Should().Be(orderId);
        payment.UserId.Should().Be(userId);
        payment.Amount.Should().Be(amount);
        payment.Currency.Should().Be(currency);
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkAsProcessing_ShouldUpdateStatus()
    {
        // Arrange
        var payment = PaymentEntity.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "TRY");

        // Act
        payment.MarkAsProcessing();

        // Assert
        payment.Status.Should().Be(PaymentStatus.Processing);
        payment.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkAsPaid_ShouldUpdateStatusAndProviderReference()
    {
        // Arrange
        var payment = PaymentEntity.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "TRY");
        var providerRef = "paytr-ref-123";

        // Act
        payment.MarkAsPaid(providerRef);

        // Assert
        payment.Status.Should().Be(PaymentStatus.Paid);
        payment.ProviderReference.Should().Be(providerRef);
    }

    [Fact]
    public void MarkAsFailed_ShouldUpdateStatusAndFailureReason()
    {
        // Arrange
        var payment = PaymentEntity.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "TRY");
        var reason = "Insufficient funds";

        // Act
        payment.MarkAsFailed(reason);

        // Assert
        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.FailureReason.Should().Be(reason);
    }

    [Fact]
    public void MarkAsRefunded_ShouldUpdateStatus()
    {
        // Arrange
        var payment = PaymentEntity.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "TRY");
        payment.MarkAsPaid("ref-123");

        // Act
        payment.MarkAsRefunded();

        // Assert
        payment.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Theory]
    [InlineData(PaymentStatus.Pending, true)]
    [InlineData(PaymentStatus.Processing, false)]
    [InlineData(PaymentStatus.Paid, false)]
    [InlineData(PaymentStatus.Failed, false)]
    public void CanProcess_ShouldReturnCorrectValue(PaymentStatus status, bool expected)
    {
        // Arrange
        var payment = PaymentEntity.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "TRY");
        
        // Set status using reflection
        typeof(PaymentEntity)
            .GetProperty("Status")!
            .SetValue(payment, status);

        // Act
        var result = payment.CanProcess();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(PaymentStatus.Pending, false)]
    [InlineData(PaymentStatus.Processing, false)]
    [InlineData(PaymentStatus.Paid, true)]
    [InlineData(PaymentStatus.Failed, true)]
    [InlineData(PaymentStatus.Cancelled, true)]
    [InlineData(PaymentStatus.Refunded, true)]
    public void IsFinalState_ShouldReturnCorrectValue(PaymentStatus status, bool expected)
    {
        // Arrange
        var payment = PaymentEntity.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "TRY");
        
        typeof(PaymentEntity)
            .GetProperty("Status")!
            .SetValue(payment, status);

        // Act
        var result = payment.IsFinalState();

        // Assert
        result.Should().Be(expected);
    }
}

public class RefundEntityTests
{
    [Fact]
    public void Create_ShouldCreateRefundWithCorrectValues()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var amount = 50m;
        var currency = "TRY";
        var reason = "Customer request";

        // Act
        var refund = RefundEntity.Create(paymentId, amount, currency, reason);

        // Assert
        refund.Should().NotBeNull();
        refund.RefundId.Should().NotBeEmpty();
        refund.PaymentId.Should().Be(paymentId);
        refund.Amount.Should().Be(amount);
        refund.Currency.Should().Be(currency);
        refund.Reason.Should().Be(reason);
        refund.Status.Should().Be(RefundStatus.Pending);
    }

    [Fact]
    public void MarkAsCompleted_ShouldUpdateStatusAndProviderReference()
    {
        // Arrange
        var refund = RefundEntity.Create(Guid.NewGuid(), 50m, "TRY");
        var providerRef = "refund-ref-123";

        // Act
        refund.MarkAsCompleted(providerRef);

        // Assert
        refund.Status.Should().Be(RefundStatus.Completed);
        refund.ProviderReference.Should().Be(providerRef);
        refund.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsFailed_ShouldUpdateStatusAndFailureReason()
    {
        // Arrange
        var refund = RefundEntity.Create(Guid.NewGuid(), 50m, "TRY");
        var reason = "Provider error";

        // Act
        refund.MarkAsFailed(reason);

        // Assert
        refund.Status.Should().Be(RefundStatus.Failed);
        refund.FailureReason.Should().Be(reason);
        refund.CompletedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(RefundStatus.Pending, false)]
    [InlineData(RefundStatus.Processing, false)]
    [InlineData(RefundStatus.Completed, true)]
    [InlineData(RefundStatus.Failed, true)]
    [InlineData(RefundStatus.Rejected, true)]
    public void IsFinalState_ShouldReturnCorrectValue(RefundStatus status, bool expected)
    {
        // Arrange
        var refund = RefundEntity.Create(Guid.NewGuid(), 50m, "TRY");
        
        typeof(RefundEntity)
            .GetProperty("Status")!
            .SetValue(refund, status);

        // Act
        var result = refund.IsFinalState();

        // Assert
        result.Should().Be(expected);
    }
}

