using PaymentFlow.Domain.Entities;
using PaymentFlow.Domain.Events;
using PaymentFlow.Domain.Exceptions;
using PaymentFlow.Domain.ValueObjects;
using Xunit;

namespace PaymentFlow.Domain.Tests.Entities;

public class PaymentTests
{
    private static readonly Guid MerchantId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 7, 13, 12, 0, 0, DateTimeKind.Utc);

    private static Payment CreatePayment(decimal amount = 100m, string currency = "USD")
    {
        return Payment.Create(
            MerchantId,
            Money.FromDecimal(amount, currency),
            IdempotencyKey.Create(Guid.NewGuid().ToString()),
            Now);
    }

    // ---------- Creation ----------

    [Fact]
    public void Create_WithValidInputs_StartsInPendingStatus()
    {
        var payment = CreatePayment(100m);

        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(MerchantId, payment.MerchantId);
        Assert.Equal(100m, payment.Amount.AsDecimal());
    }

    [Fact]
    public void Create_RaisesPaymentCreatedEvent()
    {
        var payment = CreatePayment(50m);

        var domainEvent = Assert.Single(payment.DomainEvents);
        var createdEvent = Assert.IsType<PaymentCreatedEvent>(domainEvent);
        Assert.Equal(payment.Id, createdEvent.PaymentId);
    }

    [Fact]
    public void Create_WithZeroAmount_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Payment.Create(MerchantId, Money.Zero("USD"), IdempotencyKey.Create("key-1"), Now));
    }

    [Fact]
    public void Create_WithEmptyMerchantId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Payment.Create(Guid.Empty, Money.FromDecimal(10m, "USD"), IdempotencyKey.Create("key-1"), Now));
    }

    // ---------- Happy path: Pending -> Processing -> Captured ----------

    [Fact]
    public void BeginProcessing_FromPending_TransitionsToProcessing()
    {
        var payment = CreatePayment();

        payment.BeginProcessing("FakeGateway", Now);

        Assert.Equal(PaymentStatus.Processing, payment.Status);
        Assert.Equal("FakeGateway", payment.ProviderName);
    }

    [Fact]
    public void Capture_FromProcessing_TransitionsToCapturedAndRaisesEvent()
    {
        var payment = CreatePayment();
        payment.BeginProcessing("FakeGateway", Now);

        payment.Capture("provider-ref-123", Now);

        Assert.Equal(PaymentStatus.Captured, payment.Status);
        Assert.Equal("provider-ref-123", payment.ProviderReference);
        Assert.Contains(payment.DomainEvents, e => e is PaymentCapturedEvent);
    }

    // ---------- Illegal transitions ----------

    [Fact]
    public void Capture_FromPending_ThrowsInvalidStateTransition()
    {
        var payment = CreatePayment();

        var ex = Assert.Throws<InvalidPaymentStateTransitionException>(
            () => payment.Capture("provider-ref", Now));

        Assert.Equal(PaymentStatus.Pending, ex.FromStatus);
        Assert.Equal(PaymentStatus.Captured, ex.ToStatus);
    }

    [Fact]
    public void Capture_OnAlreadyCapturedPayment_Throws()
    {
        var payment = CreatePayment();
        payment.BeginProcessing("FakeGateway", Now);
        payment.Capture("provider-ref", Now);

        Assert.Throws<InvalidPaymentStateTransitionException>(
            () => payment.Capture("provider-ref-2", Now));
    }

    [Fact]
    public void Refund_OnPendingPayment_Throws()
    {
        var payment = CreatePayment();

        Assert.Throws<InvalidPaymentStateTransitionException>(
            () => payment.Refund(Money.FromDecimal(10m, "USD"), Now));
    }

    [Fact]
    public void BeginProcessing_OnCancelledPayment_Throws()
    {
        var payment = CreatePayment();
        payment.Cancel(Now);

        Assert.Throws<InvalidPaymentStateTransitionException>(
            () => payment.BeginProcessing("FakeGateway", Now));
    }

    // ---------- Failure path ----------

    [Fact]
    public void Fail_FromProcessing_TransitionsToFailedWithReasonAndRaisesEvent()
    {
        var payment = CreatePayment();
        payment.BeginProcessing("FakeGateway", Now);

        payment.Fail("Insufficient funds", Now);

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("Insufficient funds", payment.FailureReason);
        Assert.Contains(payment.DomainEvents, e => e is PaymentFailedEvent);
    }

    // ---------- Refunds ----------

    [Fact]
    public void Refund_FullAmount_TransitionsToRefunded()
    {
        var payment = CreatePayment(100m);
        payment.BeginProcessing("FakeGateway", Now);
        payment.Capture("provider-ref", Now);

        payment.Refund(Money.FromDecimal(100m, "USD"), Now);

        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(100m, payment.RefundedAmount.AsDecimal());
    }

    [Fact]
    public void Refund_PartialAmount_TransitionsToPartiallyRefunded()
    {
        var payment = CreatePayment(100m);
        payment.BeginProcessing("FakeGateway", Now);
        payment.Capture("provider-ref", Now);

        payment.Refund(Money.FromDecimal(40m, "USD"), Now);

        Assert.Equal(PaymentStatus.PartiallyRefunded, payment.Status);
        Assert.Equal(40m, payment.RefundedAmount.AsDecimal());
    }

    [Fact]
    public void Refund_TwoPartialRefundsAddingToFullAmount_EndsAsFullyRefunded()
    {
        var payment = CreatePayment(100m);
        payment.BeginProcessing("FakeGateway", Now);
        payment.Capture("provider-ref", Now);

        payment.Refund(Money.FromDecimal(40m, "USD"), Now);
        payment.Refund(Money.FromDecimal(60m, "USD"), Now);

        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(100m, payment.RefundedAmount.AsDecimal());
    }

    [Fact]
    public void Refund_MoreThanCapturedAmount_Throws()
    {
        var payment = CreatePayment(100m);
        payment.BeginProcessing("FakeGateway", Now);
        payment.Capture("provider-ref", Now);

        Assert.Throws<InvalidOperationException>(
            () => payment.Refund(Money.FromDecimal(150m, "USD"), Now));
    }

    [Fact]
    public void Refund_RaisesPaymentRefundedEventWithCorrectFullRefundFlag()
    {
        var payment = CreatePayment(100m);
        payment.BeginProcessing("FakeGateway", Now);
        payment.Capture("provider-ref", Now);

        payment.Refund(Money.FromDecimal(100m, "USD"), Now);

        var refundEvent = Assert.Single(payment.DomainEvents.OfType<PaymentRefundedEvent>());
        Assert.True(refundEvent.IsFullRefund);
    }

    // ---------- Cancellation ----------

    [Fact]
    public void Cancel_FromPending_TransitionsToCancelled()
    {
        var payment = CreatePayment();

        payment.Cancel(Now);

        Assert.Equal(PaymentStatus.Cancelled, payment.Status);
    }

    [Fact]
    public void Cancel_OnCapturedPayment_Throws()
    {
        var payment = CreatePayment();
        payment.BeginProcessing("FakeGateway", Now);
        payment.Capture("provider-ref", Now);

        Assert.Throws<InvalidPaymentStateTransitionException>(() => payment.Cancel(Now));
    }
}