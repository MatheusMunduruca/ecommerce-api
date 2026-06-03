using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECommerceApi.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseMySql(
            "Server=localhost;Port=3306;Database=ecommerce_db;User=root;Password=REDACTED_PASSWORD;",
            ServerVersion.Parse("8.0.0-mysql"));

        return new AppDbContext(optionsBuilder.Options);
    }
}
