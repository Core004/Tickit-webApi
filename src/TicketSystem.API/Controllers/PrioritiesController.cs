using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Domain.Entities;

namespace TicketSystem.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class PrioritiesController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<PrioritiesController> _logger;

    public PrioritiesController(IApplicationDbContext context, ILogger<PrioritiesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<PriorityDto>>> GetPriorities([FromQuery] bool includeInactive = false)
    {
        var query = _context.Priorities.AsQueryable();

        if (!includeInactive)
            query = query.Where(p => p.IsActive);

        var priorities = await query
            .OrderBy(p => p.Level)
            .Select(p => new PriorityDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Color = p.Color,
                Level = p.Level,
                ResponseTimeMinutes = p.ResponseTimeMinutes,
                ResolutionTimeMinutes = p.ResolutionTimeMinutes,
                IsDefault = p.IsDefault,
                IsActive = p.IsActive
            })
            .ToListAsync();

        return Ok(priorities);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PriorityDto>> GetPriority(int id)
    {
        var priority = await _context.Priorities.FindAsync(id);
        if (priority is null)
            return NotFound();

        return Ok(new PriorityDto
        {
            Id = priority.Id,
            Name = priority.Name,
            Description = priority.Description,
            Color = priority.Color,
            Level = priority.Level,
            ResponseTimeMinutes = priority.ResponseTimeMinutes,
            ResolutionTimeMinutes = priority.ResolutionTimeMinutes,
            IsDefault = priority.IsDefault,
            IsActive = priority.IsActive
        });
    }

    [HttpPost]
    public async Task<ActionResult<int>> CreatePriority([FromBody] CreatePriorityRequest request)
    {
        var priority = new Priority
        {
            Name = request.Name,
            Description = request.Description,
            Color = request.Color,
            Level = request.Level,
            ResponseTimeMinutes = request.ResponseTimeMinutes,
            ResolutionTimeMinutes = request.ResolutionTimeMinutes,
            IsDefault = request.IsDefault,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        if (request.IsDefault)
        {
            var existingDefaults = await _context.Priorities
                .Where(p => p.IsDefault)
                .ToListAsync();
            foreach (var p in existingDefaults)
                p.IsDefault = false;
        }

        _context.Priorities.Add(priority);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Priority {Name} created with ID {Id}", priority.Name, priority.Id);

        return CreatedAtAction(nameof(GetPriority), new { id = priority.Id }, priority.Id);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePriority(int id, [FromBody] UpdatePriorityRequest request)
    {
        var priority = await _context.Priorities.FindAsync(id);
        if (priority is null)
            return NotFound();

        if (request.IsDefault && !priority.IsDefault)
        {
            var existingDefaults = await _context.Priorities
                .Where(p => p.IsDefault && p.Id != id)
                .ToListAsync();
            foreach (var p in existingDefaults)
                p.IsDefault = false;
        }

        priority.Name = request.Name;
        priority.Description = request.Description;
        priority.Color = request.Color;
        priority.Level = request.Level;
        priority.ResponseTimeMinutes = request.ResponseTimeMinutes;
        priority.ResolutionTimeMinutes = request.ResolutionTimeMinutes;
        priority.IsDefault = request.IsDefault;
        priority.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/activate")]
    public async Task<IActionResult> ActivatePriority(int id)
    {
        var priority = await _context.Priorities.FindAsync(id);
        if (priority is null)
            return NotFound();

        priority.IsActive = true;
        priority.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> DeactivatePriority(int id)
    {
        var priority = await _context.Priorities.FindAsync(id);
        if (priority is null)
            return NotFound();

        priority.IsActive = false;
        priority.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePriority(int id)
    {
        var priority = await _context.Priorities.FindAsync(id);
        if (priority is null)
            return NotFound();

        var hasTickets = await _context.Tickets.AnyAsync(t => t.PriorityId == id);
        if (hasTickets)
            return BadRequest(new { Message = "Cannot delete priority with existing tickets" });

        _context.Priorities.Remove(priority);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

// DTOs
public class PriorityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public int Level { get; set; }
    public int? ResponseTimeMinutes { get; set; }
    public int? ResolutionTimeMinutes { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

public record CreatePriorityRequest(
    string Name,
    string? Description,
    string? Color,
    int Level,
    int? ResponseTimeMinutes,
    int? ResolutionTimeMinutes,
    bool IsDefault = false);

public record UpdatePriorityRequest(
    string Name,
    string? Description,
    string? Color,
    int Level,
    int? ResponseTimeMinutes,
    int? ResolutionTimeMinutes,
    bool IsDefault = false);
