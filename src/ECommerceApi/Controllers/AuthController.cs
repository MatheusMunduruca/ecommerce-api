using System.Security.Claims;
using ECommerceApi.Data;
using ECommerceApi.DTOs;
using ECommerceApi.Models;
using ECommerceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const decimal StartingGold = 1000m;
    private const string RudolfWelcome = "Aqui, tome esse pequeno agrado, para você poder testar a qualidade de meus itens...";

    private readonly SharedDbContext _shared;
    private readonly AppDbContext _db;
    private readonly TokenService _tokenService;

    public AuthController(SharedDbContext shared, AppDbContext db, TokenService tokenService)
    {
        _shared = shared;
        _db = db;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (await _shared.Users.AnyAsync(u => u.Email == request.Email))
            return Conflict(new { message = "Email already in use." });

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            GoldBalance = StartingGold
        };

        _shared.Users.Add(user);
        await _shared.SaveChangesAsync();

        // Cria o carrinho no ecommerce_db para o novo usuário
        _db.Carts.Add(new Cart { UserId = user.Id });
        await _db.SaveChangesAsync();

        var token = _tokenService.GenerateToken(user);
        return Ok(new AuthResponse(token, user.Name, user.Email, user.GoldBalance, RudolfWelcome));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _shared.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials." });

        // Garante que o carrinho existe no ecommerce_db
        if (!await _db.Carts.AnyAsync(c => c.UserId == user.Id))
        {
            _db.Carts.Add(new Cart { UserId = user.Id });
            await _db.SaveChangesAsync();
        }

        var token = _tokenService.GenerateToken(user);
        return Ok(new AuthResponse(token, user.Name, user.Email, user.GoldBalance));
    }

    [HttpGet("gold")]
    [Authorize]
    public async Task<IActionResult> GetGold()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _shared.Users.FindAsync(userId);
        if (user is null) return NotFound();
        return Ok(new { gold = user.GoldBalance });
    }

    [HttpPost("gold/deduct")]
    [Authorize]
    public async Task<IActionResult> DeductGold([FromBody] DeductGoldRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _shared.Users.FindAsync(userId);
        if (user is null) return NotFound();

        if (user.GoldBalance < request.Amount)
            return BadRequest(new { message = "Gold insuficiente.", current = user.GoldBalance });

        user.GoldBalance -= request.Amount;
        await _shared.SaveChangesAsync();

        return Ok(new { gold = user.GoldBalance });
    }
}
