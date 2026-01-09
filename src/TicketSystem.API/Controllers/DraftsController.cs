using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Domain.Entities;

namespace TicketSystem.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class DraftsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<DraftsController> _logger;

    public DraftsController(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILogger<DraftsController> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Get all drafts for the current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<DraftDto>>> GetDrafts()
    {
        var userId = _currentUser.UserId;

        var drafts = await _context.TicketDrafts
            .Include(d => d.Category)
            .Include(d => d.Priority)
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
            .Select(d => new DraftDto
            {
                Id = d.Id,
                Title = d.Title,
                Description = d.Description,
                CategoryId = d.CategoryId,
                CategoryName = d.Category != null ? d.Category.Name : null,
                PriorityId = d.PriorityId,
                PriorityName = d.Priority != null ? d.Priority.Name : null,
                PriorityLevel = d.Priority != null ? d.Priority.Level : null,
                CompanyId = d.CompanyId,
                DepartmentId = d.DepartmentId,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            })
            .ToListAsync();

        return Ok(drafts);
    }

    /// <summary>
    /// Get a specific draft by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<DraftDto>> GetDraft(int id)
    {
        var userId = _currentUser.UserId;

        var draft = await _context.TicketDrafts
            .Include(d => d.Category)
            .Include(d => d.Priority)
            .Where(d => d.Id == id && d.UserId == userId)
            .Select(d => new DraftDto
            {
                Id = d.Id,
                Title = d.Title,
                Description = d.Description,
                CategoryId = d.CategoryId,
                CategoryName = d.Category != null ? d.Category.Name : null,
                PriorityId = d.PriorityId,
                PriorityName = d.Priority != null ? d.Priority.Name : null,
                PriorityLevel = d.Priority != null ? d.Priority.Level : null,
                CompanyId = d.CompanyId,
                DepartmentId = d.DepartmentId,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (draft is null)
            return NotFound();

        return Ok(draft);
    }

    /// <summary>
    /// Create a new draft
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<int>> CreateDraft([FromBody] CreateDraftRequest request)
    {
        var userId = _currentUser.UserId!;

        var draft = new TicketDraft
        {
            Title = request.Title ?? string.Empty,
            Description = request.Description ?? string.Empty,
            CategoryId = request.CategoryId,
            PriorityId = request.PriorityId,
            CompanyId = request.CompanyId,
            DepartmentId = request.DepartmentId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId
        };

        _context.TicketDrafts.Add(draft);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Draft {DraftId} created by {UserId}", draft.Id, userId);

        return CreatedAtAction(nameof(GetDraft), new { id = draft.Id }, draft.Id);
    }

    /// <summary>
    /// Update an existing draft
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDraft(int id, [FromBody] UpdateDraftRequest request)
    {
        var userId = _currentUser.UserId;

        var draft = await _context.TicketDrafts
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

        if (draft is null)
            return NotFound();

        draft.Title = request.Title ?? string.Empty;
        draft.Description = request.Description ?? string.Empty;
        draft.CategoryId = request.CategoryId;
        draft.PriorityId = request.PriorityId;
        draft.CompanyId = request.CompanyId;
        draft.DepartmentId = request.DepartmentId;
        draft.UpdatedAt = DateTime.UtcNow;
        draft.UpdatedById = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Draft {DraftId} updated by {UserId}", id, userId);

        return NoContent();
    }

    /// <summary>
    /// Delete a draft
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDraft(int id)
    {
        var userId = _currentUser.UserId;

        var draft = await _context.TicketDrafts
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

        if (draft is null)
            return NotFound();

        _context.TicketDrafts.Remove(draft);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Draft {DraftId} deleted by {UserId}", id, userId);

        return NoContent();
    }

    /// <summary>
    /// Convert a draft to a ticket and delete the draft
    /// </summary>
    [HttpPost("{id}/submit")]
    public async Task<ActionResult<int>> SubmitDraft(int id)
    {
        var userId = _currentUser.UserId!;

        var draft = await _context.TicketDrafts
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

        if (draft is null)
            return NotFound();

        // Validate required fields
        if (string.IsNullOrWhiteSpace(draft.Title))
            return BadRequest(new { Message = "Title is required to submit a ticket" });

        if (string.IsNullOrWhiteSpace(draft.Description))
            return BadRequest(new { Message = "Description is required to submit a ticket" });

        // Create ticket from draft
        var ticket = new Ticket
        {
            TicketNumber = $"TKT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            Title = draft.Title,
            Description = draft.Description,
            CategoryId = draft.CategoryId,
            PriorityId = draft.PriorityId,
            Priority = draft.PriorityId.HasValue
                ? (Domain.Enums.TicketPriority)draft.PriorityId.Value
                : Domain.Enums.TicketPriority.Medium,
            CompanyId = draft.CompanyId,
            DepartmentId = draft.DepartmentId,
            StatusId = 1, // Default status
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tickets.Add(ticket);

        // Delete the draft
        _context.TicketDrafts.Remove(draft);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Draft {DraftId} submitted as ticket {TicketNumber} by {UserId}",
            id, ticket.TicketNumber, userId);

        return Ok(ticket.Id);
    }
}

// DTOs
public class DraftDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? PriorityId { get; set; }
    public string? PriorityName { get; set; }
    public int? PriorityLevel { get; set; }
    public int? CompanyId { get; set; }
    public int? DepartmentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public record CreateDraftRequest(
    string? Title,
    string? Description,
    int? CategoryId,
    int? PriorityId,
    int? CompanyId,
    int? DepartmentId);

public record UpdateDraftRequest(
    string? Title,
    string? Description,
    int? CategoryId,
    int? PriorityId,
    int? CompanyId,
    int? DepartmentId);
