using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PayderPay.Infrastructure.Persistence;

public class PayderPayDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PayderPayDbContext>
{
    public PayderPayDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5157;Database=payderpay_dev;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<PayderPayDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new PayderPayDbContext(optionsBuilder.Options);
    }
}
