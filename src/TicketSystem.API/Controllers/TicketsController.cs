using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Application.Common.Models;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

namespace TicketSystem.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class TicketsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILogger<TicketsController> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedList<TicketListDto>>> GetTickets(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] int? statusId = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] TicketPriority? priority = null,
        [FromQuery] string? assignedToId = null)
    {
        var query = _context.Tickets
            .Include(t => t.Status)
            .Include(t => t.Category)
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .Where(t => !t.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(t =>
                t.Title.Contains(search) ||
                t.TicketNumber.Contains(search) ||
                t.Description.Contains(search));
        }

        if (statusId.HasValue)
            query = query.Where(t => t.StatusId == statusId);

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId);

        if (priority.HasValue)
            query = query.Where(t => t.Priority == priority);

        if (!string.IsNullOrEmpty(assignedToId))
            query = query.Where(t => t.AssignedToId == assignedToId);

        query = query.OrderByDescending(t => t.CreatedAt);

        var result = await PaginatedList<TicketListDto>.CreateAsync(
            query.Select(t => new TicketListDto
            {
                Id = t.Id,
                TicketNumber = t.TicketNumber,
                Title = t.Title,
                Priority = t.Priority,
                Status = t.Status != null ? t.Status.Name : null,
                Category = t.Category != null ? t.Category.Name : null,
                AssignedTo = t.AssignedTo != null ? t.AssignedTo.FullName : null,
                CreatedBy = t.CreatedBy != null ? t.CreatedBy.FullName : null,
                CreatedAt = t.CreatedAt,
                DueDate = t.DueDate
            }),
            pageNumber,
            pageSize);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TicketDetailDto>> GetTicket(int id)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Status)
            .Include(t => t.Category)
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .Include(t => t.Company)
            .Include(t => t.Product)
            .Include(t => t.Department)
            .Include(t => t.Team)
            .Include(t => t.Comments.OrderByDescending(c => c.CreatedAt))
                .ThenInclude(c => c.Author)
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        if (ticket is null)
            return NotFound();

        return Ok(new TicketDetailDto
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            Title = ticket.Title,
            Description = ticket.Description,
            Priority = ticket.Priority,
            StatusId = ticket.StatusId,
            Status = ticket.Status?.Name,
            CategoryId = ticket.CategoryId,
            Category = ticket.Category?.Name,
            AssignedToId = ticket.AssignedToId,
            AssignedTo = ticket.AssignedTo?.FullName,
            CreatedById = ticket.CreatedById,
            CreatedBy = ticket.CreatedBy?.FullName,
            CompanyId = ticket.CompanyId,
            Company = ticket.Company?.Name,
            ProductId = ticket.ProductId,
            Product = ticket.Product?.Name,
            DepartmentId = ticket.DepartmentId,
            Department = ticket.Department?.Name,
            TeamId = ticket.TeamId,
            Team = ticket.Team?.Name,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            DueDate = ticket.DueDate,
            FirstResponseAt = ticket.FirstResponseAt,
            ResolvedAt = ticket.ResolvedAt,
            ClosedAt = ticket.ClosedAt,
            IsSLABreached = ticket.IsSLABreached,
            Comments = ticket.Comments.Select(c => new CommentDto
            {
                Id = c.Id,
                Content = c.Content,
                IsInternal = c.IsInternal,
                AuthorId = c.AuthorId,
                Author = c.Author.FullName,
                CreatedAt = c.CreatedAt
            }).ToList(),
            AttachmentCount = ticket.Attachments.Count
        });
    }

    [HttpPost]
    public async Task<ActionResult<int>> CreateTicket([FromBody] CreateTicketRequest request)
    {
        var ticket = new Ticket
        {
            TicketNumber = $"TKT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            CategoryId = request.CategoryId,
            StatusId = request.StatusId ?? 1, // Default to first status
            CompanyId = request.CompanyId,
            ProductId = request.ProductId,
            DepartmentId = request.DepartmentId,
            CreatedById = _currentUser.UserId!,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Ticket {TicketNumber} created by {UserId}", ticket.TicketNumber, _currentUser.UserId);

        return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, ticket.Id);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTicket(int id, [FromBody] UpdateTicketRequest request)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket is null || ticket.IsDeleted)
            return NotFound();

        ticket.Title = request.Title;
        ticket.Description = request.Description;
        ticket.Priority = request.Priority;
        ticket.CategoryId = request.CategoryId;
        ticket.DueDate = request.DueDate;
        ticket.UpdatedAt = DateTime.UtcNow;
        ticket.UpdatedById = _currentUser.UserId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/assign")]
    public async Task<IActionResult> AssignTicket(int id, [FromBody] AssignTicketRequest request)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket is null || ticket.IsDeleted)
            return NotFound();

        ticket.AssignedToId = request.AssignedToId;
        ticket.UpdatedAt = DateTime.UtcNow;
        ticket.UpdatedById = _currentUser.UserId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Ticket {Id} assigned to {AssignedToId}", id, request.AssignedToId);

        return NoContent();
    }

    [HttpPost("{id}/status")]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeStatusRequest request)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket is null || ticket.IsDeleted)
            return NotFound();

        var previousStatusId = ticket.StatusId;
        ticket.StatusId = request.StatusId;
        ticket.UpdatedAt = DateTime.UtcNow;
        ticket.UpdatedById = _currentUser.UserId;

        // Check if status is resolved/closed
        var status = await _context.TicketStatuses.FindAsync(request.StatusId);
        if (status?.IsResolved == true && ticket.ResolvedAt is null)
            ticket.ResolvedAt = DateTime.UtcNow;
        if (status?.IsClosed == true && ticket.ClosedAt is null)
            ticket.ClosedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Ticket {Id} status changed from {PreviousStatus} to {NewStatus}",
            id, previousStatusId, request.StatusId);

        return NoContent();
    }

    [HttpPost("{id}/comments")]
    public async Task<ActionResult<int>> AddComment(int id, [FromBody] AddCommentRequest request)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket is null || ticket.IsDeleted)
            return NotFound();

        var comment = new TicketComment
        {
            TicketId = id,
            Content = request.Content,
            IsInternal = request.IsInternal,
            AuthorId = _currentUser.UserId!,
            CreatedAt = DateTime.UtcNow
        };

        // Update first response time if this is the first response from an agent
        if (ticket.FirstResponseAt is null && _currentUser.IsInRole("Agent"))
        {
            ticket.FirstResponseAt = DateTime.UtcNow;
        }

        _context.TicketComments.Add(comment);
        await _context.SaveChangesAsync();

        return Ok(comment.Id);
    }

    [HttpGet("{id}/comments")]
    public async Task<ActionResult<List<CommentDto>>> GetComments(int id)
    {
        var comments = await _context.TicketComments
            .Include(c => c.Author)
            .Where(c => c.TicketId == id)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommentDto
            {
                Id = c.Id,
                Content = c.Content,
                IsInternal = c.IsInternal,
                AuthorId = c.AuthorId,
                Author = c.Author.FullName,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(comments);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTicket(int id)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket is null || ticket.IsDeleted)
            return NotFound();

        ticket.IsDeleted = true;
        ticket.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Ticket {Id} soft deleted by {UserId}", id, _currentUser.UserId);

        return NoContent();
    }

    // Ticket History
    [HttpGet("{id}/history")]
    public async Task<ActionResult<List<TicketHistoryDto>>> GetTicketHistory(int id)
    {
        var history = await _context.TicketHistory
            .Include(h => h.CreatedBy)
            .Where(h => h.TicketId == id)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new TicketHistoryDto
            {
                Id = h.Id,
                FieldName = h.FieldName,
                OldValue = h.OldValue,
                NewValue = h.NewValue,
                ChangedById = h.CreatedById,
                ChangedByName = h.CreatedBy != null ? h.CreatedBy.FullName : null,
                ChangedAt = h.CreatedAt
            })
            .ToListAsync();

        return Ok(history);
    }

    // Ticket Links
    [HttpGet("{id}/links")]
    public async Task<ActionResult<List<TicketLinkDto>>> GetTicketLinks(int id)
    {
        var sourceLinks = await _context.TicketLinks
            .Include(l => l.TargetTicket)
            .Where(l => l.SourceTicketId == id)
            .Select(l => new TicketLinkDto
            {
                Id = l.Id,
                LinkedTicketId = l.TargetTicketId,
                LinkedTicketNumber = l.TargetTicket.TicketNumber,
                LinkedTicketTitle = l.TargetTicket.Title,
                LinkType = l.LinkType.ToString(),
                Direction = "Outgoing"
            })
            .ToListAsync();

        var targetLinks = await _context.TicketLinks
            .Include(l => l.SourceTicket)
            .Where(l => l.TargetTicketId == id)
            .Select(l => new TicketLinkDto
            {
                Id = l.Id,
                LinkedTicketId = l.SourceTicketId,
                LinkedTicketNumber = l.SourceTicket.TicketNumber,
                LinkedTicketTitle = l.SourceTicket.Title,
                LinkType = l.LinkType.ToString(),
                Direction = "Incoming"
            })
            .ToListAsync();

        return Ok(sourceLinks.Concat(targetLinks).ToList());
    }

    [HttpPost("{id}/links")]
    public async Task<ActionResult<int>> CreateTicketLink(int id, [FromBody] CreateTicketLinkRequest request)
    {
        var sourceTicket = await _context.Tickets.FindAsync(id);
        var targetTicket = await _context.Tickets.FindAsync(request.TargetTicketId);

        if (sourceTicket is null || sourceTicket.IsDeleted)
            return NotFound(new { Message = "Source ticket not found" });

        if (targetTicket is null || targetTicket.IsDeleted)
            return NotFound(new { Message = "Target ticket not found" });

        if (id == request.TargetTicketId)
            return BadRequest(new { Message = "Cannot link ticket to itself" });

        // Check if link already exists
        var existingLink = await _context.TicketLinks
            .FirstOrDefaultAsync(l =>
                (l.SourceTicketId == id && l.TargetTicketId == request.TargetTicketId) ||
                (l.SourceTicketId == request.TargetTicketId && l.TargetTicketId == id));

        if (existingLink != null)
            return BadRequest(new { Message = "Link already exists between these tickets" });

        var link = new TicketLink
        {
            SourceTicketId = id,
            TargetTicketId = request.TargetTicketId,
            LinkType = request.LinkType,
            CreatedById = _currentUser.UserId!,
            CreatedAt = DateTime.UtcNow
        };

        _context.TicketLinks.Add(link);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Link created between tickets {SourceId} and {TargetId}", id, request.TargetTicketId);

        return Ok(link.Id);
    }

    [HttpDelete("links/{linkId}")]
    public async Task<IActionResult> DeleteTicketLink(int linkId)
    {
        var link = await _context.TicketLinks.FindAsync(linkId);
        if (link is null)
            return NotFound();

        _context.TicketLinks.Remove(link);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Merge Tickets
    [HttpPost("{id}/merge")]
    public async Task<IActionResult> MergeTickets(int id, [FromBody] MergeTicketsRequest request)
    {
        var targetTicket = await _context.Tickets.FindAsync(id);
        if (targetTicket is null || targetTicket.IsDeleted)
            return NotFound(new { Message = "Target ticket not found" });

        var sourceTickets = await _context.Tickets
            .Where(t => request.SourceTicketIds.Contains(t.Id) && !t.IsDeleted)
            .ToListAsync();

        if (!sourceTickets.Any())
            return BadRequest(new { Message = "No valid source tickets found" });

        foreach (var sourceTicket in sourceTickets)
        {
            sourceTicket.MergedIntoTicketId = id;
            sourceTicket.IsDeleted = true;
            sourceTicket.DeletedAt = DateTime.UtcNow;

            // Add merge comment to target ticket
            var comment = new TicketComment
            {
                TicketId = id,
                Content = $"Merged from ticket {sourceTicket.TicketNumber}: {sourceTicket.Title}",
                IsSystemGenerated = true,
                AuthorId = _currentUser.UserId!,
                CreatedAt = DateTime.UtcNow
            };
            _context.TicketComments.Add(comment);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Tickets {SourceIds} merged into {TargetId}",
            string.Join(", ", request.SourceTicketIds), id);

        return NoContent();
    }
}

// DTOs
public class TicketListDto
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public TicketPriority Priority { get; set; }
    public string? Status { get; set; }
    public string? Category { get; set; }
    public string? AssignedTo { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
}

public class TicketDetailDto : TicketListDto
{
    public string Description { get; set; } = string.Empty;
    public int? StatusId { get; set; }
    public int? CategoryId { get; set; }
    public string? AssignedToId { get; set; }
    public string CreatedById { get; set; } = string.Empty;
    public int? CompanyId { get; set; }
    public string? Company { get; set; }
    public int? ProductId { get; set; }
    public string? Product { get; set; }
    public int? DepartmentId { get; set; }
    public string? Department { get; set; }
    public int? TeamId { get; set; }
    public string? Team { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? FirstResponseAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool IsSLABreached { get; set; }
    public List<CommentDto> Comments { get; set; } = new();
    public int AttachmentCount { get; set; }
}

public class CommentDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public record CreateTicketRequest(
    string Title,
    string Description,
    TicketPriority Priority,
    int? CategoryId,
    int? StatusId,
    int? CompanyId,
    int? ProductId,
    int? DepartmentId);

public record UpdateTicketRequest(
    string Title,
    string Description,
    TicketPriority Priority,
    int? CategoryId,
    DateTime? DueDate);

public record AssignTicketRequest(string? AssignedToId);
public record ChangeStatusRequest(int StatusId);
public record AddCommentRequest(string Content, bool IsInternal = false);

public class TicketHistoryDto
{
    public int Id { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? ChangedById { get; set; }
    public string? ChangedByName { get; set; }
    public DateTime ChangedAt { get; set; }
}

public class TicketLinkDto
{
    public int Id { get; set; }
    public int LinkedTicketId { get; set; }
    public string LinkedTicketNumber { get; set; } = string.Empty;
    public string LinkedTicketTitle { get; set; } = string.Empty;
    public string LinkType { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
}

public record CreateTicketLinkRequest(int TargetTicketId, TicketLinkType LinkType);
public record MergeTicketsRequest(List<int> SourceTicketIds);
