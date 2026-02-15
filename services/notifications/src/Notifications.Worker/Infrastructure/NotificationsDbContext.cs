using CommonKernel.Persistence;
using Microsoft.EntityFrameworkCore;
using Notifications.Worker.Domain;

namespace Notifications.Worker.Infrastructure;

public sealed class NotificationsDbContext : DbContext, IInboxDbContext
{
    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
        : base(options)
    {
    }

    public DbSet<NotificationLog> Notifications => Set<NotificationLog>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("notifications");

        modelBuilder.Entity<NotificationLog>(entity =>
        {
            entity.ToTable("notification_logs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Channel).HasMaxLength(30);
            entity.Property(x => x.Destination).HasMaxLength(120);
            entity.Property(x => x.Message).HasMaxLength(500);
            entity.HasIndex(x => x.EventId).IsUnique();
            entity.HasIndex(x => x.PaymentId);
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
