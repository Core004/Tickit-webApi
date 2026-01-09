using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Application.Common.Models;
using TicketSystem.Domain.Entities;

namespace TicketSystem.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(IApplicationDbContext context, ILogger<CategoriesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories([FromQuery] bool includeInactive = false)
    {
        var query = _context.TicketCategories
            .Include(c => c.ParentCategory)
            .AsQueryable();

        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        var categories = await query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Color = c.Color,
                Icon = c.Icon,
                ParentCategoryId = c.ParentCategoryId,
                ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.Name : null,
                DisplayOrder = c.DisplayOrder,
                IsActive = c.IsActive
            })
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDetailDto>> GetCategory(int id)
    {
        var category = await _context.TicketCategories
            .Include(c => c.ParentCategory)
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
            return NotFound();

        return Ok(new CategoryDetailDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Color = category.Color,
            Icon = category.Icon,
            ParentCategoryId = category.ParentCategoryId,
            ParentCategoryName = category.ParentCategory?.Name,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive,
            SubCategories = category.SubCategories.Select(s => new CategoryDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Color = s.Color,
                DisplayOrder = s.DisplayOrder,
                IsActive = s.IsActive
            }).ToList()
        });
    }

    [HttpPost]
    public async Task<ActionResult<int>> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        var category = new TicketCategory
        {
            Name = request.Name,
            Description = request.Description,
            Color = request.Color,
            Icon = request.Icon,
            ParentCategoryId = request.ParentCategoryId,
            DisplayOrder = request.DisplayOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.TicketCategories.Add(category);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Category {Name} created with ID {Id}", category.Name, category.Id);

        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category.Id);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
    {
        var category = await _context.TicketCategories.FindAsync(id);
        if (category is null)
            return NotFound();

        category.Name = request.Name;
        category.Description = request.Description;
        category.Color = request.Color;
        category.Icon = request.Icon;
        category.ParentCategoryId = request.ParentCategoryId;
        category.DisplayOrder = request.DisplayOrder;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/activate")]
    public async Task<IActionResult> ActivateCategory(int id)
    {
        var category = await _context.TicketCategories.FindAsync(id);
        if (category is null)
            return NotFound();

        category.IsActive = true;
        category.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> DeactivateCategory(int id)
    {
        var category = await _context.TicketCategories.FindAsync(id);
        if (category is null)
            return NotFound();

        category.IsActive = false;
        category.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.TicketCategories.FindAsync(id);
        if (category is null)
            return NotFound();

        // Check if category has tickets
        var hasTickets = await _context.Tickets.AnyAsync(t => t.CategoryId == id);
        if (hasTickets)
            return BadRequest(new { Message = "Cannot delete category with existing tickets" });

        _context.TicketCategories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

// DTOs
public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public int? ParentCategoryId { get; set; }
    public string? ParentCategoryName { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class CategoryDetailDto : CategoryDto
{
    public List<CategoryDto> SubCategories { get; set; } = new();
}

public record CreateCategoryRequest(
    string Name,
    string? Description,
    string? Color,
    string? Icon,
    int? ParentCategoryId,
    int DisplayOrder = 0);

public record UpdateCategoryRequest(
    string Name,
    string? Description,
    string? Color,
    string? Icon,
    int? ParentCategoryId,
    int DisplayOrder = 0);
