using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayderPay.Domain.Entities;

namespace PayderPay.Infrastructure.Persistence.Configurations;

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("NotificationLogs", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_NotificationLogs_PeriodMonth", "\"PeriodMonth\" >= 1 AND \"PeriodMonth\" <= 12");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PeriodYear)
            .IsRequired();

        builder.Property(x => x.PeriodMonth)
            .IsRequired();

        builder.Property(x => x.Channel)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Recipient)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(x => x.Subject)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.FailureReason)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => new { x.SubscriptionId, x.PeriodYear, x.PeriodMonth, x.Channel, x.Status });

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
