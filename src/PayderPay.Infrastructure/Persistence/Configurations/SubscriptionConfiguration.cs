using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayderPay.Domain.Entities;

namespace PayderPay.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_Subscriptions_DueDayOfMonth", "\"DueDayOfMonth\" >= 1 AND \"DueDayOfMonth\" <= 31");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SubscriptionType)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.ProviderName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.SubscriberNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.DueDayOfMonth)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => new { x.CustomerId, x.ProviderName, x.SubscriberNumber })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasMany(x => x.Payments)
            .WithOne(x => x.Subscription)
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.DebtQueryResults)
            .WithOne(x => x.Subscription)
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.NotificationLogs)
            .WithOne(x => x.Subscription)
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
