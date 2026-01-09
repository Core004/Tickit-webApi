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
public class NotificationsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILogger<NotificationsController> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedList<NotificationDto>>> GetNotifications(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isRead = null)
    {
        var userId = _currentUser.UserId;

        var query = _context.Notifications
            .Where(n => n.UserId == userId)
            .AsQueryable();

        if (isRead.HasValue)
            query = query.Where(n => n.IsRead == isRead.Value);

        query = query.OrderByDescending(n => n.CreatedAt);

        var result = await PaginatedList<NotificationDto>.CreateAsync(
            query.Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                CreatedAt = n.CreatedAt,
                Link = n.Link
            }),
            pageNumber, pageSize);

        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var userId = _currentUser.UserId;
        var count = await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);

        return Ok(count);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<NotificationDto>> GetNotification(int id)
    {
        var userId = _currentUser.UserId;
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification is null)
            return NotFound();

        return Ok(new NotificationDto
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            IsRead = notification.IsRead,
            ReadAt = notification.ReadAt,
            CreatedAt = notification.CreatedAt,
            Link = notification.Link
        });
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = _currentUser.UserId;
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification is null)
            return NotFound();

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = _currentUser.UserId;
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        var userId = _currentUser.UserId;
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification is null)
            return NotFound();

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("clear-all")]
    public async Task<IActionResult> ClearAllNotifications()
    {
        var userId = _currentUser.UserId;
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .ToListAsync();

        _context.Notifications.RemoveRange(notifications);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Admin endpoint to send notification
    [HttpPost]
    public async Task<ActionResult<int>> CreateNotification([FromBody] CreateNotificationRequest request)
    {
        var notification = new Notification
        {
            UserId = request.UserId,
            Title = request.Title,
            Message = request.Message,
            Type = request.Type ?? "info",
            Link = request.Link,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Notification created for user {UserId}", request.UserId);

        return CreatedAtAction(nameof(GetNotification), new { id = notification.Id }, notification.Id);
    }
}

// DTOs
public class NotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Link { get; set; }
}

public record CreateNotificationRequest(
    string UserId,
    string Title,
    string Message,
    string? Type = null,
    string? Link = null);
