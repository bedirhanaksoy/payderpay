using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayderPay.Domain.Entities;

namespace PayderPay.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_Payments_PeriodMonth", "\"PeriodMonth\" >= 1 AND \"PeriodMonth\" <= 12");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.PaymentDateUtc)
            .IsRequired();

        builder.Property(x => x.PeriodYear)
            .IsRequired();

        builder.Property(x => x.PeriodMonth)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.ExternalTransactionId)
            .HasMaxLength(100);

        builder.Property(x => x.FailureReason)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => new { x.SubscriptionId, x.PeriodYear, x.PeriodMonth })
            .IsUnique()
            .HasFilter("\"Status\" = 'Successful' AND \"IsDeleted\" = false");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
