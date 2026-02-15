namespace Risk.Worker.Domain;

public sealed class RiskAssessmentRecord
{
    public long Id { get; set; }
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int Score { get; set; }
    public bool Approved { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset EvaluatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
