using CommonKernel.Persistence;
using Microsoft.EntityFrameworkCore;
using Payments.Api.Domain;

namespace Payments.Api.Infrastructure;

public sealed class PaymentsDbContext : DbContext, IOutboxDbContext, IInboxDbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options)
        : base(options)
    {
    }

    public DbSet<PaymentIntent> PaymentIntents => Set<PaymentIntent>();
    public DbSet<RiskAssessment> RiskAssessments => Set<RiskAssessment>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("payments");

        modelBuilder.Entity<PaymentIntent>(entity =>
        {
            entity.ToTable("payment_intents");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasColumnType("numeric(18,2)");
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.PayerId).HasMaxLength(80);
            entity.Property(x => x.MerchantId).HasMaxLength(80);
            entity.Property(x => x.CorrelationId).HasMaxLength(80);
            entity.Property(x => x.LastReason).HasMaxLength(250);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.HasIndex(x => x.CreatedAtUtc);
        });

        modelBuilder.Entity<RiskAssessment>(entity =>
        {
            entity.ToTable("risk_assessments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Reason).HasMaxLength(200);
            entity.HasIndex(x => x.PaymentId).IsUnique();
        });

        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.ToTable("idempotency_records");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Key).HasMaxLength(120);
            entity.Property(x => x.RequestHash).HasMaxLength(200);
            entity.HasIndex(x => x.Key).IsUnique();
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(120);
            entity.Property(x => x.CorrelationId).HasMaxLength(80);
            entity.Property(x => x.Source).HasMaxLength(60);
            entity.Property(x => x.Payload).HasColumnType("jsonb");
            entity.HasIndex(x => x.PublishedAtUtc);
        });

        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.ToTable("inbox_messages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(120);
            entity.Property(x => x.Consumer).HasMaxLength(80);
            entity.Property(x => x.Error).HasMaxLength(500);
            entity.HasIndex(x => new { x.EventId, x.Consumer }).IsUnique();
        });
    }
}
