using CommonKernel.Payments;
using Payments.Api.Domain;

namespace Payments.UnitTests;

public sealed class PaymentIntentTests
{
    [Fact]
    public void Create_ShouldStartPendingRisk()
    {
        var payment = PaymentIntent.Create(100, "brl", "payer", "merchant", "corr-1");

        payment.Status.Should().Be(PaymentStatus.PendingRisk);
        payment.Currency.Should().Be("BRL");
    }

    [Fact]
    public void RequestReversal_WhenNotPosted_ShouldReturnFalse()
    {
        var payment = PaymentIntent.Create(100, "USD", "payer", "merchant", "corr-1");

        var result = payment.RequestReversal("client requested");

        result.Should().BeFalse();
        payment.Status.Should().Be(PaymentStatus.PendingRisk);
    }
}
