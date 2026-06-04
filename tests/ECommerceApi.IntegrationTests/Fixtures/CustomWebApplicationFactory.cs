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

namespace ECommerceApi.IntegrationTests.Fixtures;

/// <summary>
/// Sobe a API real em memória para testes de integração:
/// - troca AppDbContext e SharedDbContext por bancos InMemory (isolados por instância);
/// - injeta configuração de JWT/conexão (independe do appsettings.json — funciona em CI);
/// - remove o serviço de background de estoque;
/// - semeia categorias, produtos e a janela de estoque atual (estoque determinístico).
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _appDb = "app-" + Guid.NewGuid();
    private readonly string _sharedDb = "shared-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "chave-de-teste-bem-longa-hmacsha256-integration-0123456789",
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
            services.RemoveAll<IHostedService>(); // remove o StockRandomizerHostedService

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
        Seed(db);

        return host;
    }

    private static void Seed(AppDbContext db)
    {
        if (db.Categories.Any()) return;

        var pocoes = new Category { Name = "Pocoes" };
        var ingredientes = new Category { Name = "Ingredientes" };
        db.Categories.AddRange(pocoes, ingredientes);

        db.Products.AddRange(
            new Product { Name = "Pocao de Cura Menor", Description = "Cura 50", Price = 50, StockQuantity = 10, DiscountPercent = 0, Category = pocoes },
            new Product { Name = "Pocao com Desconto", Description = "Promo", Price = 100, StockQuantity = 10, DiscountPercent = 25, Category = pocoes },
            new Product { Name = "Erva da Lua", Description = "Rara", Price = 25, StockQuantity = 2, DiscountPercent = 0, Category = ingredientes }
        );

        // Marca a janela atual como já sorteada para o estoque permanecer determinístico
        db.StockStates.Add(new StockState
        {
            LastWindow = RudolfStockService.CurrentWindow(),
            LastRandomizedAt = DateTime.UtcNow
        });

        db.SaveChanges();
    }
}
