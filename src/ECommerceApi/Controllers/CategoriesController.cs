using AutoMapper;
using ECommerceApi.Data;
using ECommerceApi.DTOs;
using ECommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApi.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public CategoriesController(AppDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _db.Categories.ToListAsync();
        return Ok(_mapper.Map<List<CategoryResponse>>(categories));
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(CreateCategoryRequest request)
    {
        var category = new Category { Name = request.Name };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), _mapper.Map<CategoryResponse>(category));
    }
}
