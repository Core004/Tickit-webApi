using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Domain.Entities;

namespace TicketSystem.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class AuditLogController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AuditLogController> _logger;

    public AuditLogController(IApplicationDbContext context, ILogger<AuditLogController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedAuditLogResponse>> GetAuditLogs(
        [FromQuery] string? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.AuditLogs
            .Include(a => a.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(a => a.UserId == userId);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action.ToLower().Contains(action.ToLower()));

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType.ToLower() == entityType.ToLower());

        if (!string.IsNullOrWhiteSpace(entityId))
            query = query.Where(a => a.EntityId == entityId);

        if (startDate.HasValue)
            query = query.Where(a => a.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.Timestamp <= endDate.Value);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.UserName,
                UserEmail = a.User != null ? a.User.Email : null,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent,
                Timestamp = a.Timestamp
            })
            .ToListAsync();

        return Ok(new PaginatedAuditLogResponse
        {
            Items = logs,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AuditLogDetailDto>> GetAuditLog(int id)
    {
        var log = await _context.AuditLogs
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (log is null)
            return NotFound();

        return Ok(new AuditLogDetailDto
        {
            Id = log.Id,
            UserId = log.UserId,
            UserName = log.UserName,
            UserEmail = log.User?.Email,
            Action = log.Action,
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            OldValues = log.OldValues,
            NewValues = log.NewValues,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            Timestamp = log.Timestamp
        });
    }

    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<ActionResult<List<AuditLogDto>>> GetEntityAuditHistory(string entityType, string entityId)
    {
        var logs = await _context.AuditLogs
            .Where(a => a.EntityType.ToLower() == entityType.ToLower() && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.UserName,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent,
                Timestamp = a.Timestamp
            })
            .ToListAsync();

        return Ok(logs);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<AuditLogDto>>> GetUserAuditHistory(string userId, [FromQuery] int limit = 100)
    {
        var logs = await _context.AuditLogs
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.UserName,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent,
                Timestamp = a.Timestamp
            })
            .ToListAsync();

        return Ok(logs);
    }

    [HttpGet("actions")]
    public async Task<ActionResult<List<string>>> GetDistinctActions()
    {
        var actions = await _context.AuditLogs
            .Select(a => a.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();

        return Ok(actions);
    }

    [HttpGet("entity-types")]
    public async Task<ActionResult<List<string>>> GetDistinctEntityTypes()
    {
        var entityTypes = await _context.AuditLogs
            .Select(a => a.EntityType)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync();

        return Ok(entityTypes);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AuditLogSummaryDto>> GetAuditLogSummary(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (startDate.HasValue)
            query = query.Where(a => a.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.Timestamp <= endDate.Value);

        var totalLogs = await query.CountAsync();

        var actionBreakdown = await query
            .GroupBy(a => a.Action)
            .Select(g => new ActionBreakdownItem
            {
                Action = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        var entityTypeBreakdown = await query
            .GroupBy(a => a.EntityType)
            .Select(g => new EntityTypeBreakdownItem
            {
                EntityType = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        var mostActiveUsers = await query
            .Where(a => a.UserId != null)
            .GroupBy(a => new { a.UserId, a.UserName })
            .Select(g => new ActiveUserItem
            {
                UserId = g.Key.UserId!,
                UserName = g.Key.UserName,
                ActionCount = g.Count()
            })
            .OrderByDescending(x => x.ActionCount)
            .Take(10)
            .ToListAsync();

        return Ok(new AuditLogSummaryDto
        {
            TotalLogs = totalLogs,
            ActionBreakdown = actionBreakdown,
            EntityTypeBreakdown = entityTypeBreakdown,
            MostActiveUsers = mostActiveUsers
        });
    }

    [HttpPost]
    public async Task<ActionResult<int>> CreateAuditLog([FromBody] CreateAuditLogRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userName = User.Identity?.Name ?? "System";

        var log = new AuditLog
        {
            UserId = userId,
            UserName = userName,
            Action = request.Action,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            OldValues = request.OldValues,
            NewValues = request.NewValues,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers["User-Agent"].ToString(),
            Timestamp = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Audit log created: {Action} on {EntityType} {EntityId}",
            log.Action, log.EntityType, log.EntityId);

        return CreatedAtAction(nameof(GetAuditLog), new { id = log.Id }, log.Id);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAuditLog(int id)
    {
        var log = await _context.AuditLogs.FindAsync(id);
        if (log is null)
            return NotFound();

        _context.AuditLogs.Remove(log);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("cleanup")]
    public async Task<ActionResult<int>> CleanupOldLogs([FromQuery] int olderThanDays = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);

        var logsToDelete = await _context.AuditLogs
            .Where(a => a.Timestamp < cutoffDate)
            .ToListAsync();

        var deletedCount = logsToDelete.Count;

        _context.AuditLogs.RemoveRange(logsToDelete);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Cleaned up {Count} audit logs older than {Days} days", deletedCount, olderThanDays);

        return Ok(new { DeletedCount = deletedCount });
    }

    [HttpGet("export")]
    public async Task<ActionResult> ExportAuditLogs(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string format = "json")
    {
        var query = _context.AuditLogs.AsQueryable();

        if (startDate.HasValue)
            query = query.Where(a => a.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.Timestamp <= endDate.Value);

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Select(a => new AuditLogExportDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.UserName,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                IpAddress = a.IpAddress,
                Timestamp = a.Timestamp
            })
            .ToListAsync();

        if (format.ToLower() == "csv")
        {
            var csv = GenerateCsv(logs);
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", $"audit_logs_{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        // Default to JSON
        return Ok(logs);
    }

    private static string GenerateCsv(List<AuditLogExportDto> logs)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Id,UserId,UserName,Action,EntityType,EntityId,IpAddress,Timestamp");

        foreach (var log in logs)
        {
            sb.AppendLine($"{log.Id},{log.UserId},{EscapeCsv(log.UserName)},{EscapeCsv(log.Action)},{EscapeCsv(log.EntityType)},{log.EntityId},{log.IpAddress},{log.Timestamp:yyyy-MM-dd HH:mm:ss}");
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }
}

// DTOs
public class AuditLogDto
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
}

public class AuditLogDetailDto : AuditLogDto { }

public class AuditLogExportDto
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; }
}

public class PaginatedAuditLogResponse
{
    public List<AuditLogDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class AuditLogSummaryDto
{
    public int TotalLogs { get; set; }
    public List<ActionBreakdownItem> ActionBreakdown { get; set; } = new();
    public List<EntityTypeBreakdownItem> EntityTypeBreakdown { get; set; } = new();
    public List<ActiveUserItem> MostActiveUsers { get; set; } = new();
}

public class ActionBreakdownItem
{
    public string Action { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class EntityTypeBreakdownItem
{
    public string EntityType { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ActiveUserItem
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int ActionCount { get; set; }
}

public record CreateAuditLogRequest(
    string Action,
    string EntityType,
    string? EntityId = null,
    string? OldValues = null,
    string? NewValues = null);
