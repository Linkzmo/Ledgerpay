using CommonKernel.Contracts.Events;

namespace Risk.Worker.Services;

public sealed class RiskDecisionService
{
    public (bool Approved, int Score, string Reason) Evaluate(PaymentCreatedEvent evt)
    {
        var hashBase = Math.Abs(evt.PaymentId.GetHashCode());
        var score = hashBase % 100;

        if (evt.Amount > 20000)
        {
            return (false, score, "Amount above manual threshold.");
        }

        if (score < 25)
        {
            return (false, score, "Score below approval threshold.");
        }

        return (true, score, "Approved by risk engine.");
    }
}
