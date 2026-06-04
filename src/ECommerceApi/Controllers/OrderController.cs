using System.Security.Claims;
using ECommerceApi.Data;
using ECommerceApi.DTOs;
using ECommerceApi.Enums;
using ECommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApi.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly AppDbContext _db;

    public OrderController(AppDbContext db) => _db = db;

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>POST /api/orders — Cria um pedido a partir do carrinho atual.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrder()
    {
        var userId = GetUserId();

        var cart = await _db.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null || !cart.Items.Any())
            return BadRequest(new { message = "Seu carrinho está vazio." });

        // Verifica estoque de todos os itens
        foreach (var item in cart.Items)
        {
            if (item.Product is null)
                return BadRequest(new { message = $"Produto ID {item.ProductId} não encontrado." });

            if (item.Product.StockQuantity < item.Quantity)
                return BadRequest(new
                {
                    message = $"Estoque insuficiente para '{item.Product.Name}'. Disponível: {item.Product.StockQuantity}."
                });
        }

        // Cria o pedido (preço final já com desconto)
        var total = cart.Items.Sum(i => i.Product!.FinalPrice * i.Quantity);
        var order = new Order
        {
            UserId = userId,
            Status = OrderStatus.Pending,
            TotalAmount = total
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // Adiciona itens do pedido e debita estoque
        foreach (var item in cart.Items)
        {
            _db.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.Product!.FinalPrice
            });

            item.Product.StockQuantity -= item.Quantity;
        }

        // Limpa o carrinho
        _db.CartItems.RemoveRange(cart.Items);
        await _db.SaveChangesAsync();

        // Recarrega para retornar
        var created = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstAsync(o => o.Id == order.Id);

        return Ok(MapOrder(created));
    }

    /// <summary>GET /api/orders — Lista pedidos do usuário autenticado.</summary>
    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var userId = GetUserId();
        var orders = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Ok(orders.Select(MapOrder));
    }

    /// <summary>GET /api/orders/{id} — Detalhe de um pedido.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var userId = GetUserId();
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

        if (order is null) return NotFound();
        return Ok(MapOrder(order));
    }

    private static OrderResponse MapOrder(Order o)
    {
        var items = o.Items.Select(i => new OrderItemResponse(
            i.ProductId,
            i.Product?.Name ?? "Desconhecido",
            i.UnitPrice,
            i.Quantity,
            i.UnitPrice * i.Quantity
        )).ToList();

        return new OrderResponse(o.Id, o.UserId, o.Status.ToString(), o.TotalAmount, o.CreatedAt, items);
    }
}
