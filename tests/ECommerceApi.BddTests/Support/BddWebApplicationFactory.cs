using ECommerceApi.Data;
using ECommerceApi.Models;
using ECommerceApi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace ECommerceApi.BddTests.Support;

/// <summary>API real em memória para os cenários BDD (mesma estratégia da integração).</summary>
public class BddWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _appDb = "bdd-app-" + Guid.NewGuid();
    private readonly string _sharedDb = "bdd-shared-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "chave-de-teste-bem-longa-hmacsha256-bdd-0123456789abcdef",
                ["Jwt:Issuer"] = "TodoApi",
                ["Jwt:Audience"] = "TodoApiUsers",
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=t;User=root;Password=t;",
                ["ConnectionStrings:SharedConnection"] = "Server=localhost;Database=t;User=root;Password=t;",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<SharedDbContext>>();
            services.RemoveAll<SharedDbContext>();
            services.RemoveAll<IHostedService>();

            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_appDb));
            services.AddDbContext<SharedDbContext>(o => o.UseInMemoryDatabase(_sharedDb));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        if (!db.Categories.Any())
        {
            var pocoes = new Category { Name = "Pocoes" };
            var ingredientes = new Category { Name = "Ingredientes" };
            db.Categories.AddRange(pocoes, ingredientes);
            db.Products.AddRange(
                new Product { Name = "Pocao de Cura Menor", Description = "Cura", Price = 50, StockQuantity = 10, DiscountPercent = 0, Category = pocoes },
                new Product { Name = "Erva da Lua", Description = "Rara", Price = 25, StockQuantity = 2, DiscountPercent = 0, Category = ingredientes }
            );
            db.StockStates.Add(new StockState { LastWindow = RudolfStockService.CurrentWindow(), LastRandomizedAt = DateTime.UtcNow });
            db.SaveChanges();
        }

        return host;
    }
}
