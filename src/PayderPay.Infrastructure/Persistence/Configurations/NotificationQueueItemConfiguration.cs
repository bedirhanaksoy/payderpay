using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayderPay.Domain.Entities;

namespace PayderPay.Infrastructure.Persistence.Configurations;

public class NotificationQueueItemConfiguration : IEntityTypeConfiguration<NotificationQueueItem>
{
    public void Configure(EntityTypeBuilder<NotificationQueueItem> builder)
    {
        builder.ToTable("notification_queue");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.InvoiceId)
            .HasColumnName("invoice_id")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.NotificationType)
            .HasColumnName("notification_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ScheduledFor)
            .HasColumnName("scheduled_for")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.Attempts)
            .HasColumnName("attempts")
            .IsRequired();

        builder.Property(x => x.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(2000);

        builder.Property(x => x.SentAt)
            .HasColumnName("sent_at");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique();

        builder.HasIndex(x => new { x.Status, x.ScheduledFor, x.Attempts });

        builder.HasOne(x => x.Invoice)
            .WithMany(x => x.NotificationQueueItems)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.User)
            .WithMany(x => x.NotificationQueueItems)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
