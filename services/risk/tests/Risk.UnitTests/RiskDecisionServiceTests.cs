using CommonKernel.Contracts.Events;
using Risk.Worker.Services;

namespace Risk.UnitTests;

public sealed class RiskDecisionServiceTests
{
    private readonly RiskDecisionService _service = new();

    [Fact]
    public void Evaluate_ShouldRejectVeryHighAmount()
    {
        var evt = new PaymentCreatedEvent(Guid.NewGuid(), 50000, "USD", "payer", "merchant", DateTimeOffset.UtcNow);

        var result = _service.Evaluate(evt);

        result.Approved.Should().BeFalse();
        result.Reason.Should().Contain("threshold");
    }
}
