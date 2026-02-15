using CommonKernel.Persistence;
using Microsoft.EntityFrameworkCore;
using Risk.Worker.Domain;

namespace Risk.Worker.Infrastructure;

public sealed class RiskDbContext : DbContext, IInboxDbContext, IOutboxDbContext
{
    public RiskDbContext(DbContextOptions<RiskDbContext> options)
        : base(options)
    {
    }

    public DbSet<RiskAssessmentRecord> RiskAssessments => Set<RiskAssessmentRecord>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("risk");

        modelBuilder.Entity<RiskAssessmentRecord>(entity =>
        {
            entity.ToTable("risk_assessments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasColumnType("numeric(18,2)");
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.Reason).HasMaxLength(200);
            entity.HasIndex(x => x.PaymentId).IsUnique();
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
    }
}
