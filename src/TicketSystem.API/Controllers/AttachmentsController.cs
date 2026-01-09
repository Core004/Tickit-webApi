using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Domain.Entities;

namespace TicketSystem.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class AttachmentsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AttachmentsController> _logger;

    public AttachmentsController(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IWebHostEnvironment environment,
        ILogger<AttachmentsController> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet("ticket/{ticketId}")]
    public async Task<ActionResult<List<AttachmentDto>>> GetTicketAttachments(int ticketId)
    {
        var attachments = await _context.TicketAttachments
            .Include(a => a.UploadedBy)
            .Where(a => a.TicketId == ticketId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AttachmentDto
            {
                Id = a.Id,
                FileName = a.FileName,
                OriginalFileName = a.OriginalFileName,
                ContentType = a.ContentType,
                FileSize = a.FileSize,
                UploadedById = a.UploadedById,
                UploadedByName = a.UploadedBy != null ? a.UploadedBy.FullName : null,
                UploadedAt = a.CreatedAt
            })
            .ToListAsync();

        return Ok(attachments);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AttachmentDto>> GetAttachment(int id)
    {
        var attachment = await _context.TicketAttachments
            .Include(a => a.UploadedBy)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attachment is null)
            return NotFound();

        return Ok(new AttachmentDto
        {
            Id = attachment.Id,
            FileName = attachment.FileName,
            OriginalFileName = attachment.OriginalFileName,
            ContentType = attachment.ContentType,
            FileSize = attachment.FileSize,
            UploadedById = attachment.UploadedById,
            UploadedByName = attachment.UploadedBy?.FullName,
            UploadedAt = attachment.CreatedAt
        });
    }

    [HttpPost("ticket/{ticketId}")]
    public async Task<ActionResult<int>> UploadAttachment(int ticketId, IFormFile file)
    {
        var ticket = await _context.Tickets.FindAsync(ticketId);
        if (ticket is null || ticket.IsDeleted)
            return NotFound(new { Message = "Ticket not found" });

        if (file is null || file.Length == 0)
            return BadRequest(new { Message = "No file provided" });

        // Validate file size (max 10MB)
        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { Message = "File size exceeds 10MB limit" });

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".zip" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { Message = "File type not allowed" });

        // Create uploads directory
        var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads", "tickets", ticketId.ToString());
        Directory.CreateDirectory(uploadsPath);

        // Generate unique filename
        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsPath, fileName);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var attachment = new TicketAttachment
        {
            TicketId = ticketId,
            FileName = fileName,
            OriginalFileName = file.FileName,
            FilePath = filePath,
            ContentType = file.ContentType,
            FileSize = file.Length,
            UploadedById = _currentUser.UserId!,
            CreatedAt = DateTime.UtcNow
        };

        _context.TicketAttachments.Add(attachment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Attachment {FileName} uploaded to ticket {TicketId}", file.FileName, ticketId);

        return CreatedAtAction(nameof(GetAttachment), new { id = attachment.Id }, attachment.Id);
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadAttachment(int id)
    {
        var attachment = await _context.TicketAttachments.FindAsync(id);
        if (attachment is null)
            return NotFound();

        if (!System.IO.File.Exists(attachment.FilePath))
            return NotFound(new { Message = "File not found on disk" });

        var memory = new MemoryStream();
        using (var stream = new FileStream(attachment.FilePath, FileMode.Open))
        {
            await stream.CopyToAsync(memory);
        }
        memory.Position = 0;

        return File(memory, attachment.ContentType, attachment.OriginalFileName);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAttachment(int id)
    {
        var attachment = await _context.TicketAttachments.FindAsync(id);
        if (attachment is null)
            return NotFound();

        // Delete file from disk
        if (System.IO.File.Exists(attachment.FilePath))
        {
            System.IO.File.Delete(attachment.FilePath);
        }

        _context.TicketAttachments.Remove(attachment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Attachment {Id} deleted", id);

        return NoContent();
    }
}

// DTOs
public class AttachmentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? UploadedById { get; set; }
    public string? UploadedByName { get; set; }
    public DateTime UploadedAt { get; set; }
}
