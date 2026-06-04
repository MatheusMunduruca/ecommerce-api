using System.Security.Claims;
using AutoMapper;
using ECommerceApi.Data;
using ECommerceApi.DTOs;
using ECommerceApi.Models;
using ECommerceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApi.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly RudolfStockService _stock;

    public ProductsController(AppDbContext db, IMapper mapper, RudolfStockService stock)
    {
        _db = db;
        _mapper = mapper;
        _stock = stock;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? category, [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice)
    {
        // Re-sorteia o estoque se a janela de 6h virou (cobre API que ficou offline)
        await _stock.EnsureCurrentAsync();

        var query = _db.Products.Include(p => p.Category).AsQueryable();

        if (category.HasValue)
            query = query.Where(p => p.CategoryId == category.Value);

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        var products = await query.ToListAsync();
        return Ok(_mapper.Map<List<ProductResponse>>(products));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        await _stock.EnsureCurrentAsync();

        var product = await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return NotFound();
        return Ok(_mapper.Map<ProductResponse>(product));
    }

    /// <summary>
    /// Força a renovação imediata do estoque de Rudolf.
    /// Restrito a contas administrativas (domínio de e-mail "adm").
    /// </summary>
    [HttpPost("refresh-stock")]
    [Authorize]
    public async Task<IActionResult> RefreshStock()
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var domain = email.Contains('@') ? email.Split('@')[1] : string.Empty;

        if (!domain.Equals("adm", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        await _stock.ForceRandomizeAsync();
        return Ok(new { message = "Estoque renovado." });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(CreateProductRequest request)
    {
        if (!await _db.Categories.AnyAsync(c => c.Id == request.CategoryId))
            return BadRequest(new { message = "Category not found." });

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            CategoryId = request.CategoryId
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        await _db.Entry(product).Reference(p => p.Category).LoadAsync();
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, _mapper.Map<ProductResponse>(product));
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, UpdateProductRequest request)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return NotFound();

        if (!await _db.Categories.AnyAsync(c => c.Id == request.CategoryId))
            return BadRequest(new { message = "Category not found." });

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.CategoryId = request.CategoryId;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return NotFound();

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
