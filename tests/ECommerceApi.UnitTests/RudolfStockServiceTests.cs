using ECommerceApi.Data;
using ECommerceApi.Models;
using ECommerceApi.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApi.UnitTests;

/// <summary>
/// Testes do RudolfStockService usando EF Core InMemory.
/// Valida as faixas de estoque por categoria e os valores de desconto permitidos.
/// </summary>
public class RudolfStockServiceTests
{
    // Faixa máxima de estoque por categoria (mínimo é sempre 0)
    private static readonly Dictionary<string, int> MaxStock = new()
    {
        ["Pocoes"] = 25,
        ["Ingredientes"] = 50,
        ["Grimorios"] = 10,
        ["Equipamentos"] = 5,
    };

    private static readonly int[] DescontosValidos = { 0, 5, 10, 25 };

    private static AppDbContext NewDbWithProducts()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"stock-{Guid.NewGuid()}")
            .Options;
        var db = new AppDbContext(options);

        var pocoes = new Category { Name = "Pocoes" };
        var ingredientes = new Category { Name = "Ingredientes" };
        var grimorios = new Category { Name = "Grimorios" };
        var equipamentos = new Category { Name = "Equipamentos" };
        db.Categories.AddRange(pocoes, ingredientes, grimorios, equipamentos);

        db.Products.AddRange(
            new Product { Name = "Poção de Cura", Price = 50, Category = pocoes },
            new Product { Name = "Erva da Lua", Price = 25, Category = ingredientes },
            new Product { Name = "Grimório das Runas", Price = 400, Category = grimorios },
            new Product { Name = "Alambique", Price = 250, Category = equipamentos }
        );
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task ForceRandomize_ColocaEstoqueDentroDasFaixasPorCategoria()
    {
        using var db = NewDbWithProducts();
        var service = new RudolfStockService(db);

        await service.ForceRandomizeAsync();

        var products = await db.Products.Include(p => p.Category).ToListAsync();
        foreach (var p in products)
        {
            var max = MaxStock[p.Category.Name];
            p.StockQuantity.Should().BeInRange(0, max,
                $"o estoque de '{p.Name}' ({p.Category.Name}) deve ficar entre 0 e {max}");
        }
    }

    [Fact]
    public async Task ForceRandomize_AplicaApenasDescontosPermitidos()
    {
        using var db = NewDbWithProducts();
        var service = new RudolfStockService(db);

        await service.ForceRandomizeAsync();

        var products = await db.Products.ToListAsync();
        products.Should().OnlyContain(p => DescontosValidos.Contains(p.DiscountPercent));
    }

    [Fact]
    public async Task EnsureCurrent_RegistraAJanelaAtual()
    {
        using var db = NewDbWithProducts();
        var service = new RudolfStockService(db);

        await service.EnsureCurrentAsync();

        var state = await db.StockStates.SingleAsync();
        state.LastWindow.Should().Be(RudolfStockService.CurrentWindow());
    }

    [Fact]
    public async Task EnsureCurrent_NaoReSorteiaDentroDaMesmaJanela()
    {
        using var db = NewDbWithProducts();
        var service = new RudolfStockService(db);

        await service.EnsureCurrentAsync();
        var primeiraRodada = await db.Products
            .OrderBy(p => p.Id)
            .Select(p => p.StockQuantity)
            .ToListAsync();

        await service.EnsureCurrentAsync(); // mesma janela → não deve mudar

        var segundaRodada = await db.Products
            .OrderBy(p => p.Id)
            .Select(p => p.StockQuantity)
            .ToListAsync();

        segundaRodada.Should().Equal(primeiraRodada);
    }
}
