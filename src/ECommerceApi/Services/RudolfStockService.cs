using ECommerceApi.Data;
using ECommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApi.Services;

/// <summary>
/// Randomiza o estoque e os descontos das mercadorias de Rudolf a cada 6 horas.
///
/// Funciona mesmo que a API tenha ficado fechada: o "relógio" é a janela de 6h
/// (índice = segundos desde 1970 / 21600). Se a janela atual difere da última
/// registrada no banco, re-sorteia tudo — independentemente de quanto tempo
/// a API ficou offline.
///
/// Regras de estoque por categoria:
///   Poções        → 0 a 25
///   Ingredientes  → 0 a 50
///   Grimórios     → 0 a 10
///   Equipamentos  → 0 a 5
///
/// Probabilidade de um item receber desconto:
///   Poções 7%, Ingredientes 12%, Grimórios 3%, Equipamentos 1%
/// Valor do desconto (quando aplicado):
///   5% (70%), 10% (25%), 25% (5%)
/// </summary>
public class RudolfStockService
{
    private const int WindowSeconds = 6 * 3600; // 6 horas
    private static readonly SemaphoreSlim _gate = new(1, 1);

    private readonly AppDbContext _db;

    public RudolfStockService(AppDbContext db) => _db = db;

    public static long CurrentWindow() =>
        DateTimeOffset.UtcNow.ToUnixTimeSeconds() / WindowSeconds;

    private static (int min, int max, double discountChance) RulesFor(string category)
    {
        var c = category.ToLowerInvariant();
        if (c.Contains("poç") || c.Contains("poc")) return (0, 25, 0.07);
        if (c.Contains("ingred"))                    return (0, 50, 0.12);
        if (c.Contains("grim"))                      return (0, 10, 0.03);
        if (c.Contains("equip"))                     return (0, 5,  0.01);
        return (0, 10, 0.0);
    }

    private static int RollDiscountValue(Random r)
    {
        var p = r.NextDouble();
        if (p < 0.70) return 5;   // 70%
        if (p < 0.95) return 10;  // 25%
        return 25;                // 5%
    }

    /// <summary>
    /// Garante que o estoque corresponde à janela de 6h atual.
    /// Se a janela mudou, re-sorteia estoque e descontos de todos os produtos.
    /// </summary>
    public async Task EnsureCurrentAsync()
    {
        var current = CurrentWindow();

        // Checagem rápida sem travar
        var existing = await _db.StockStates.AsNoTracking().FirstOrDefaultAsync();
        if (existing != null && existing.LastWindow == current) return;

        await _gate.WaitAsync();
        try
        {
            // Recarrega dentro do lock para evitar randomização dupla em corrida
            var state = await _db.StockStates.FirstOrDefaultAsync();
            if (state != null && state.LastWindow == current) return;

            // Semente baseada na janela: estável dentro das 6h, muda a cada janela
            await ApplyRandomizationAsync(new Random(unchecked((int)current)), current);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Força um novo sorteio do estoque imediatamente (uso administrativo).
    /// Usa semente aleatória para garantir valores diferentes do sorteio da janela.
    /// </summary>
    public async Task ForceRandomizeAsync()
    {
        await _gate.WaitAsync();
        try
        {
            await ApplyRandomizationAsync(new Random(), CurrentWindow());
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task ApplyRandomizationAsync(Random rng, long window)
    {
        var products = await _db.Products.Include(p => p.Category).ToListAsync();
        foreach (var p in products)
        {
            var (min, max, chance) = RulesFor(p.Category?.Name ?? "");
            p.StockQuantity = rng.Next(min, max + 1);
            p.DiscountPercent = rng.NextDouble() < chance ? RollDiscountValue(rng) : 0;
        }

        var state = await _db.StockStates.FirstOrDefaultAsync();
        if (state == null)
        {
            state = new StockState();
            _db.StockStates.Add(state);
        }
        state.LastWindow = window;
        state.LastRandomizedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }
}
