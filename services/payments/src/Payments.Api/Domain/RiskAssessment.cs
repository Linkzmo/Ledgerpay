namespace Payments.Api.Domain;

public sealed class RiskAssessment
{
    public long Id { get; set; }
    public Guid PaymentId { get; set; }
    public int Score { get; set; }
    public bool Approved { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset EvaluatedAtUtc { get; set; }
}
