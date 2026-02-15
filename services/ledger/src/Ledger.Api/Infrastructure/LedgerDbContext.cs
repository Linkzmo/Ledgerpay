using CommonKernel.Persistence;
using Ledger.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Api.Infrastructure;

public sealed class LedgerDbContext : DbContext, IInboxDbContext, IOutboxDbContext
{
    public LedgerDbContext(DbContextOptions<LedgerDbContext> options)
        : base(options)
    {
    }

    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<PaymentLedgerSnapshot> PaymentSnapshots => Set<PaymentLedgerSnapshot>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("ledger");

        modelBuilder.Entity<LedgerEntry>(entity =>
        {
            entity.ToTable("ledger_entries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Account).HasMaxLength(120);
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.Operation).HasMaxLength(30);
            entity.Property(x => x.Amount).HasColumnType("numeric(18,2)");
            entity.Property(x => x.EntryType).HasConversion<int>();
            entity.HasIndex(x => x.PaymentId);
            entity.HasIndex(x => x.CreatedAtUtc);
        });

        modelBuilder.Entity<PaymentLedgerSnapshot>(entity =>
        {
            entity.ToTable("payment_snapshots");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasColumnType("numeric(18,2)");
            entity.Property(x => x.Currency).HasMaxLength(3);
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
