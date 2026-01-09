using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Common.Interfaces;

namespace TicketSystem.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class PermissionsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<PermissionsController> _logger;

    public PermissionsController(IApplicationDbContext context, ILogger<PermissionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<PermissionGroupDto>>> GetPermissions()
    {
        var permissions = await _context.Permissions
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ToListAsync();

        var grouped = permissions
            .GroupBy(p => p.Category ?? "General")
            .Select(g => new PermissionGroupDto
            {
                Category = g.Key,
                Permissions = g.Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Category = p.Category
                }).ToList()
            })
            .ToList();

        return Ok(grouped);
    }

    [HttpGet("flat")]
    public async Task<ActionResult<List<PermissionDto>>> GetPermissionsFlat()
    {
        var permissions = await _context.Permissions
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .Select(p => new PermissionDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Category = p.Category
            })
            .ToListAsync();

        return Ok(permissions);
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetCategories()
    {
        var categories = await _context.Permissions
            .Where(p => p.Category != null)
            .Select(p => p.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return Ok(categories);
    }
}

// DTOs
public class PermissionGroupDto
{
    public string Category { get; set; } = string.Empty;
    public List<PermissionDto> Permissions { get; set; } = new();
}
