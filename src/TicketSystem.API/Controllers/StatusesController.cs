using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Domain.Entities;

namespace TicketSystem.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class StatusesController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<StatusesController> _logger;

    public StatusesController(IApplicationDbContext context, ILogger<StatusesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<StatusDto>>> GetStatuses([FromQuery] bool includeInactive = false)
    {
        var query = _context.TicketStatuses.AsQueryable();

        if (!includeInactive)
            query = query.Where(s => s.IsActive);

        var statuses = await query
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Name)
            .Select(s => new StatusDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Color = s.Color,
                DisplayOrder = s.DisplayOrder,
                IsDefault = s.IsDefault,
                IsResolved = s.IsResolved,
                IsClosed = s.IsClosed,
                IsActive = s.IsActive
            })
            .ToListAsync();

        return Ok(statuses);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StatusDto>> GetStatus(int id)
    {
        var status = await _context.TicketStatuses.FindAsync(id);
        if (status is null)
            return NotFound();

        return Ok(new StatusDto
        {
            Id = status.Id,
            Name = status.Name,
            Description = status.Description,
            Color = status.Color,
            DisplayOrder = status.DisplayOrder,
            IsDefault = status.IsDefault,
            IsResolved = status.IsResolved,
            IsClosed = status.IsClosed,
            IsActive = status.IsActive
        });
    }

    [HttpPost]
    public async Task<ActionResult<int>> CreateStatus([FromBody] CreateStatusRequest request)
    {
        var status = new TicketStatusEntity
        {
            Name = request.Name,
            Description = request.Description,
            Color = request.Color,
            DisplayOrder = request.DisplayOrder,
            IsDefault = request.IsDefault,
            IsResolved = request.IsResolved,
            IsClosed = request.IsClosed,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // If this is set as default, remove default from others
        if (request.IsDefault)
        {
            var existingDefaults = await _context.TicketStatuses
                .Where(s => s.IsDefault)
                .ToListAsync();
            foreach (var s in existingDefaults)
                s.IsDefault = false;
        }

        _context.TicketStatuses.Add(status);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Status {Name} created with ID {Id}", status.Name, status.Id);

        return CreatedAtAction(nameof(GetStatus), new { id = status.Id }, status.Id);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        var status = await _context.TicketStatuses.FindAsync(id);
        if (status is null)
            return NotFound();

        // If setting as default, remove default from others
        if (request.IsDefault && !status.IsDefault)
        {
            var existingDefaults = await _context.TicketStatuses
                .Where(s => s.IsDefault && s.Id != id)
                .ToListAsync();
            foreach (var s in existingDefaults)
                s.IsDefault = false;
        }

        status.Name = request.Name;
        status.Description = request.Description;
        status.Color = request.Color;
        status.DisplayOrder = request.DisplayOrder;
        status.IsDefault = request.IsDefault;
        status.IsResolved = request.IsResolved;
        status.IsClosed = request.IsClosed;
        status.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/activate")]
    public async Task<IActionResult> ActivateStatus(int id)
    {
        var status = await _context.TicketStatuses.FindAsync(id);
        if (status is null)
            return NotFound();

        status.IsActive = true;
        status.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> DeactivateStatus(int id)
    {
        var status = await _context.TicketStatuses.FindAsync(id);
        if (status is null)
            return NotFound();

        status.IsActive = false;
        status.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStatus(int id)
    {
        var status = await _context.TicketStatuses.FindAsync(id);
        if (status is null)
            return NotFound();

        var hasTickets = await _context.Tickets.AnyAsync(t => t.StatusId == id);
        if (hasTickets)
            return BadRequest(new { Message = "Cannot delete status with existing tickets" });

        _context.TicketStatuses.Remove(status);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

// DTOs
public class StatusDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsResolved { get; set; }
    public bool IsClosed { get; set; }
    public bool IsActive { get; set; }
}

public record CreateStatusRequest(
    string Name,
    string? Description,
    string? Color,
    int DisplayOrder = 0,
    bool IsDefault = false,
    bool IsResolved = false,
    bool IsClosed = false);

public record UpdateStatusRequest(
    string Name,
    string? Description,
    string? Color,
    int DisplayOrder = 0,
    bool IsDefault = false,
    bool IsResolved = false,
    bool IsClosed = false);
