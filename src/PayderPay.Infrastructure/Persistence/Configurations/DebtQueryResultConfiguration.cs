using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayderPay.Domain.Entities;

namespace PayderPay.Infrastructure.Persistence.Configurations;

public class DebtQueryResultConfiguration : IEntityTypeConfiguration<DebtQueryResult>
{
    public void Configure(EntityTypeBuilder<DebtQueryResult> builder)
    {
        builder.ToTable("DebtQueryResults", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_DebtQueryResults_PeriodMonth", "\"PeriodMonth\" >= 1 AND \"PeriodMonth\" <= 12");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.DueDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.PeriodYear)
            .IsRequired();

        builder.Property(x => x.PeriodMonth)
            .IsRequired();

        builder.Property(x => x.QueriedAtUtc)
            .IsRequired();

        builder.Property(x => x.ProviderRef)
            .HasMaxLength(100);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => new { x.SubscriptionId, x.QueriedAtUtc });

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
