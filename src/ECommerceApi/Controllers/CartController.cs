using System.Security.Claims;
using ECommerceApi.Data;
using ECommerceApi.DTOs;
using ECommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApi.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly AppDbContext _db;

    public CartController(AppDbContext db) => _db = db;

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Cart> GetOrCreateCartAsync(int userId)
    {
        var cart = await _db.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null)
        {
            cart = new Cart { UserId = userId };
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();

            // Recarrega para ter navegação correta
            cart = await _db.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                .FirstAsync(c => c.UserId == userId);
        }
        return cart;
    }

    /// <summary>GET /api/cart — Retorna o carrinho do usuário autenticado.</summary>
    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = GetUserId();
        var cart = await GetOrCreateCartAsync(userId);
        return Ok(MapCart(cart));
    }

    /// <summary>POST /api/cart/items — Adiciona ou incrementa um produto no carrinho.</summary>
    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest req)
    {
        if (req.Quantity <= 0)
            return BadRequest(new { message = "Quantidade deve ser maior que zero." });

        var product = await _db.Products.FindAsync(req.ProductId);
        if (product is null)
            return NotFound(new { message = "Produto não encontrado." });

        if (product.StockQuantity <= 0)
            return BadRequest(new { message = "Produto fora de estoque." });

        var userId = GetUserId();
        var cart = await GetOrCreateCartAsync(userId);

        var existing = cart.Items.FirstOrDefault(i => i.ProductId == req.ProductId);
        if (existing is not null)
        {
            var newQty = existing.Quantity + req.Quantity;
            if (newQty > product.StockQuantity)
                return BadRequest(new { message = "Estoque insuficiente para essa quantidade." });
            existing.Quantity = newQty;
        }
        else
        {
            if (req.Quantity > product.StockQuantity)
                return BadRequest(new { message = "Estoque insuficiente para essa quantidade." });

            cart.Items.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = req.ProductId,
                Quantity = req.Quantity
            });
        }

        await _db.SaveChangesAsync();

        // Recarrega com navigations
        cart = await GetOrCreateCartAsync(userId);
        return Ok(MapCart(cart));
    }

    /// <summary>DELETE /api/cart/items/{productId} — Remove um item do carrinho.</summary>
    [HttpDelete("items/{productId}")]
    public async Task<IActionResult> RemoveItem(int productId)
    {
        var userId = GetUserId();
        var cart = await GetOrCreateCartAsync(userId);

        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item is null)
            return NotFound(new { message = "Item não encontrado no carrinho." });

        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync();

        cart = await GetOrCreateCartAsync(userId);
        return Ok(MapCart(cart));
    }

    /// <summary>DELETE /api/cart — Limpa o carrinho inteiro.</summary>
    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        var userId = GetUserId();
        var cart = await GetOrCreateCartAsync(userId);

        _db.CartItems.RemoveRange(cart.Items);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Carrinho esvaziado." });
    }

    private static CartResponse MapCart(Cart cart)
    {
        var items = cart.Items.Select(i =>
        {
            var unit = i.Product?.FinalPrice ?? 0;   // já com desconto aplicado
            return new CartItemResponse(
                i.ProductId,
                i.Product?.Name ?? "Desconhecido",
                unit,
                i.Quantity,
                unit * i.Quantity
            );
        }).ToList();

        var total = items.Sum(i => i.Subtotal);

        return new CartResponse(cart.Id, cart.UserId, items, total);
    }
}
