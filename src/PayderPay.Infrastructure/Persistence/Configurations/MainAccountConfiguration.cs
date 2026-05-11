using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayderPay.Domain.Entities;

namespace PayderPay.Infrastructure.Persistence.Configurations;

public class MainAccountConfiguration : IEntityTypeConfiguration<MainAccount>
{
    public void Configure(EntityTypeBuilder<MainAccount> builder)
    {
        builder.ToTable("MainAccounts", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_MainAccounts_Balance", "\"Balance\" >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Iban)
            .IsRequired()
            .HasMaxLength(34);

        builder.Property(x => x.Balance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.CustomerId)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => x.Iban)
            .IsUnique();

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
