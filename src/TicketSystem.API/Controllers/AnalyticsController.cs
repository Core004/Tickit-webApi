using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

namespace TicketSystem.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class AnalyticsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<AnalyticsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> GetDashboard()
    {
        var today = DateTime.UtcNow.Date;
        var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
        var thisMonthStart = new DateTime(today.Year, today.Month, 1);

        // Total counts
        var totalTickets = await _context.Tickets.CountAsync(t => !t.IsDeleted);
        var openTickets = await _context.Tickets.CountAsync(t => !t.IsDeleted && t.Status != null && !t.Status.IsClosed);
        var resolvedTickets = await _context.Tickets.CountAsync(t => !t.IsDeleted && t.Status != null && t.Status.IsResolved);
        var closedTickets = await _context.Tickets.CountAsync(t => !t.IsDeleted && t.Status != null && t.Status.IsClosed);

        // Today's tickets
        var ticketsToday = await _context.Tickets.CountAsync(t => !t.IsDeleted && t.CreatedAt.Date == today);
        var resolvedToday = await _context.Tickets.CountAsync(t => !t.IsDeleted && t.ResolvedAt != null && t.ResolvedAt.Value.Date == today);

        // This week
        var ticketsThisWeek = await _context.Tickets.CountAsync(t => !t.IsDeleted && t.CreatedAt >= thisWeekStart);
        var resolvedThisWeek = await _context.Tickets.CountAsync(t => !t.IsDeleted && t.ResolvedAt != null && t.ResolvedAt >= thisWeekStart);

        // This month
        var ticketsThisMonth = await _context.Tickets.CountAsync(t => !t.IsDeleted && t.CreatedAt >= thisMonthStart);
        var resolvedThisMonth = await _context.Tickets.CountAsync(t => !t.IsDeleted && t.ResolvedAt != null && t.ResolvedAt >= thisMonthStart);

        // By priority
        var ticketsByPriority = await _context.Tickets
            .Where(t => !t.IsDeleted)
            .GroupBy(t => t.Priority)
            .Select(g => new { Priority = g.Key, Count = g.Count() })
            .ToListAsync();

        // By status
        var ticketsByStatus = await _context.Tickets
            .Include(t => t.Status)
            .Where(t => !t.IsDeleted && t.Status != null)
            .GroupBy(t => t.Status!.Name)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        // SLA breached
        var slaBreached = await _context.Tickets.CountAsync(t => !t.IsDeleted && t.IsSLABreached);

        // Unassigned tickets
        var unassigned = await _context.Tickets.CountAsync(t => !t.IsDeleted && t.AssignedToId == null);

        // Average resolution time (in hours)
        var resolvedTicketsWithTime = await _context.Tickets
            .Where(t => !t.IsDeleted && t.ResolvedAt != null)
            .Select(t => new { t.CreatedAt, t.ResolvedAt })
            .ToListAsync();

        double avgResolutionHours = 0;
        if (resolvedTicketsWithTime.Any())
        {
            avgResolutionHours = resolvedTicketsWithTime
                .Average(t => (t.ResolvedAt!.Value - t.CreatedAt).TotalHours);
        }

        // User counts
        var totalUsers = await _userManager.Users.CountAsync();
        var activeUsers = await _userManager.Users.CountAsync(u => u.IsActive);

        return Ok(new DashboardDto
        {
            TotalTickets = totalTickets,
            OpenTickets = openTickets,
            ResolvedTickets = resolvedTickets,
            ClosedTickets = closedTickets,
            TicketsToday = ticketsToday,
            ResolvedToday = resolvedToday,
            TicketsThisWeek = ticketsThisWeek,
            ResolvedThisWeek = resolvedThisWeek,
            TicketsThisMonth = ticketsThisMonth,
            ResolvedThisMonth = resolvedThisMonth,
            UnassignedTickets = unassigned,
            SLABreachedTickets = slaBreached,
            AverageResolutionHours = Math.Round(avgResolutionHours, 2),
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            TicketsByPriority = ticketsByPriority.ToDictionary(x => x.Priority.ToString(), x => x.Count),
            TicketsByStatus = ticketsByStatus.ToDictionary(x => x.Status, x => x.Count)
        });
    }

    [HttpGet("tickets/trend")]
    public async Task<ActionResult<List<TrendDataDto>>> GetTicketTrend(
        [FromQuery] int days = 30)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-days);

        var createdByDay = await _context.Tickets
            .Where(t => !t.IsDeleted && t.CreatedAt >= startDate)
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Created = g.Count() })
            .ToListAsync();

        var resolvedByDay = await _context.Tickets
            .Where(t => !t.IsDeleted && t.ResolvedAt != null && t.ResolvedAt >= startDate)
            .GroupBy(t => t.ResolvedAt!.Value.Date)
            .Select(g => new { Date = g.Key, Resolved = g.Count() })
            .ToListAsync();

        var result = new List<TrendDataDto>();
        for (var date = startDate; date <= DateTime.UtcNow.Date; date = date.AddDays(1))
        {
            result.Add(new TrendDataDto
            {
                Date = date,
                Created = createdByDay.FirstOrDefault(x => x.Date == date)?.Created ?? 0,
                Resolved = resolvedByDay.FirstOrDefault(x => x.Date == date)?.Resolved ?? 0
            });
        }

        return Ok(result);
    }

    [HttpGet("tickets/by-category")]
    public async Task<ActionResult<List<CategoryStatsDto>>> GetTicketsByCategory()
    {
        var stats = await _context.Tickets
            .Include(t => t.Category)
            .Where(t => !t.IsDeleted)
            .GroupBy(t => new { t.CategoryId, CategoryName = t.Category != null ? t.Category.Name : "Uncategorized" })
            .Select(g => new CategoryStatsDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.CategoryName,
                TotalTickets = g.Count(),
                OpenTickets = g.Count(t => t.Status != null && !t.Status.IsClosed),
                ClosedTickets = g.Count(t => t.Status != null && t.Status.IsClosed)
            })
            .OrderByDescending(x => x.TotalTickets)
            .ToListAsync();

        return Ok(stats);
    }

    [HttpGet("tickets/by-agent")]
    public async Task<ActionResult<List<AgentStatsDto>>> GetTicketsByAgent()
    {
        var stats = await _context.Tickets
            .Include(t => t.AssignedTo)
            .Where(t => !t.IsDeleted && t.AssignedToId != null)
            .GroupBy(t => new { t.AssignedToId, AgentName = t.AssignedTo!.FirstName + " " + t.AssignedTo.LastName })
            .Select(g => new AgentStatsDto
            {
                AgentId = g.Key.AssignedToId!,
                AgentName = g.Key.AgentName,
                TotalAssigned = g.Count(),
                OpenTickets = g.Count(t => t.Status != null && !t.Status.IsClosed),
                ResolvedTickets = g.Count(t => t.Status != null && t.Status.IsResolved),
                ClosedTickets = g.Count(t => t.Status != null && t.Status.IsClosed)
            })
            .OrderByDescending(x => x.TotalAssigned)
            .ToListAsync();

        return Ok(stats);
    }

    [HttpGet("tickets/by-company")]
    public async Task<ActionResult<List<CompanyStatsDto>>> GetTicketsByCompany()
    {
        var stats = await _context.Tickets
            .Include(t => t.Company)
            .Where(t => !t.IsDeleted && t.CompanyId != null)
            .GroupBy(t => new { t.CompanyId, CompanyName = t.Company!.Name })
            .Select(g => new CompanyStatsDto
            {
                CompanyId = g.Key.CompanyId!.Value,
                CompanyName = g.Key.CompanyName,
                TotalTickets = g.Count(),
                OpenTickets = g.Count(t => t.Status != null && !t.Status.IsClosed),
                ClosedTickets = g.Count(t => t.Status != null && t.Status.IsClosed)
            })
            .OrderByDescending(x => x.TotalTickets)
            .ToListAsync();

        return Ok(stats);
    }

    [HttpGet("sla/performance")]
    public async Task<ActionResult<SLAPerformanceDto>> GetSLAPerformance()
    {
        var totalTickets = await _context.Tickets.CountAsync(t => !t.IsDeleted);
        var breachedTickets = await _context.Tickets.CountAsync(t => !t.IsDeleted && t.IsSLABreached);
        var withinSLA = totalTickets - breachedTickets;

        var slaComplianceRate = totalTickets > 0
            ? Math.Round((double)withinSLA / totalTickets * 100, 2)
            : 100;

        return Ok(new SLAPerformanceDto
        {
            TotalTickets = totalTickets,
            WithinSLA = withinSLA,
            BreachedSLA = breachedTickets,
            ComplianceRate = slaComplianceRate
        });
    }

    [HttpGet("tickets")]
    public async Task<ActionResult<TicketAnalyticsDto>> GetTicketAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
        var end = endDate ?? DateTime.UtcNow;

        var tickets = _context.Tickets
            .Include(t => t.Status)
            .Include(t => t.Category)
            .Where(t => !t.IsDeleted && t.CreatedAt >= start && t.CreatedAt <= end);

        var totalCount = await tickets.CountAsync();
        var openCount = await tickets.CountAsync(t => t.Status != null && !t.Status.IsClosed);
        var resolvedCount = await tickets.CountAsync(t => t.Status != null && t.Status.IsResolved);
        var closedCount = await tickets.CountAsync(t => t.Status != null && t.Status.IsClosed);
        var breachedCount = await tickets.CountAsync(t => t.IsSLABreached);

        // By priority breakdown
        var byPriority = await tickets
            .GroupBy(t => t.Priority)
            .Select(g => new { Priority = g.Key, Count = g.Count() })
            .ToListAsync();

        // By category breakdown
        var byCategory = await tickets
            .GroupBy(t => t.Category != null ? t.Category.Name : "Uncategorized")
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        // Average times
        var resolvedTickets = await tickets
            .Where(t => t.ResolvedAt != null)
            .Select(t => new { t.CreatedAt, t.ResolvedAt, t.FirstResponseAt })
            .ToListAsync();

        double avgResolutionHours = 0;
        double avgFirstResponseHours = 0;
        if (resolvedTickets.Any())
        {
            avgResolutionHours = resolvedTickets.Average(t => (t.ResolvedAt!.Value - t.CreatedAt).TotalHours);
            var withResponse = resolvedTickets.Where(t => t.FirstResponseAt != null).ToList();
            if (withResponse.Any())
                avgFirstResponseHours = withResponse.Average(t => (t.FirstResponseAt!.Value - t.CreatedAt).TotalHours);
        }

        return Ok(new TicketAnalyticsDto
        {
            StartDate = start,
            EndDate = end,
            TotalTickets = totalCount,
            OpenTickets = openCount,
            ResolvedTickets = resolvedCount,
            ClosedTickets = closedCount,
            SLABreachedTickets = breachedCount,
            AverageResolutionHours = Math.Round(avgResolutionHours, 2),
            AverageFirstResponseHours = Math.Round(avgFirstResponseHours, 2),
            ByPriority = byPriority.ToDictionary(x => x.Priority.ToString(), x => x.Count),
            ByCategory = byCategory.ToDictionary(x => x.Category, x => x.Count)
        });
    }

    [HttpGet("performance")]
    public async Task<ActionResult<TeamPerformanceDto>> GetTeamPerformance(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
        var end = endDate ?? DateTime.UtcNow;

        // Get all agents with assigned tickets in the period
        var agentPerformance = await _context.Tickets
            .Include(t => t.AssignedTo)
            .Include(t => t.Status)
            .Where(t => !t.IsDeleted && t.AssignedToId != null && t.CreatedAt >= start && t.CreatedAt <= end)
            .GroupBy(t => new { t.AssignedToId, AgentName = t.AssignedTo!.FirstName + " " + t.AssignedTo.LastName })
            .Select(g => new AgentPerformanceDto
            {
                AgentId = g.Key.AssignedToId!,
                AgentName = g.Key.AgentName,
                TotalAssigned = g.Count(),
                Resolved = g.Count(t => t.Status != null && t.Status.IsResolved),
                Closed = g.Count(t => t.Status != null && t.Status.IsClosed),
                SLABreached = g.Count(t => t.IsSLABreached),
                Open = g.Count(t => t.Status != null && !t.Status.IsClosed)
            })
            .OrderByDescending(x => x.Resolved)
            .ToListAsync();

        // Calculate resolution rates
        foreach (var agent in agentPerformance)
        {
            agent.ResolutionRate = agent.TotalAssigned > 0
                ? Math.Round((double)(agent.Resolved + agent.Closed) / agent.TotalAssigned * 100, 2)
                : 0;
        }

        // Team-level metrics
        var totalAssigned = agentPerformance.Sum(a => a.TotalAssigned);
        var totalResolved = agentPerformance.Sum(a => a.Resolved);
        var totalClosed = agentPerformance.Sum(a => a.Closed);
        var totalBreached = agentPerformance.Sum(a => a.SLABreached);

        return Ok(new TeamPerformanceDto
        {
            StartDate = start,
            EndDate = end,
            TotalTicketsHandled = totalAssigned,
            TotalResolved = totalResolved,
            TotalClosed = totalClosed,
            TotalSLABreached = totalBreached,
            OverallResolutionRate = totalAssigned > 0
                ? Math.Round((double)(totalResolved + totalClosed) / totalAssigned * 100, 2)
                : 0,
            AgentPerformance = agentPerformance
        });
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportAnalytics(
        [FromQuery] string format = "json",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
        var end = endDate ?? DateTime.UtcNow;

        var tickets = await _context.Tickets
            .Include(t => t.Status)
            .Include(t => t.Category)
            .Include(t => t.AssignedTo)
            .Include(t => t.Company)
            .Where(t => !t.IsDeleted && t.CreatedAt >= start && t.CreatedAt <= end)
            .Select(t => new TicketExportDto
            {
                TicketNumber = t.TicketNumber,
                Title = t.Title,
                Status = t.Status != null ? t.Status.Name : "Unknown",
                Priority = t.Priority.ToString(),
                Category = t.Category != null ? t.Category.Name : "Uncategorized",
                AssignedTo = t.AssignedTo != null ? t.AssignedTo.FirstName + " " + t.AssignedTo.LastName : "Unassigned",
                Company = t.Company != null ? t.Company.Name : "N/A",
                CreatedAt = t.CreatedAt,
                ResolvedAt = t.ResolvedAt,
                ClosedAt = t.ClosedAt,
                IsSLABreached = t.IsSLABreached
            })
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        if (format.ToLower() == "csv")
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("TicketNumber,Title,Status,Priority,Category,AssignedTo,Company,CreatedAt,ResolvedAt,ClosedAt,IsSLABreached");

            foreach (var ticket in tickets)
            {
                csv.AppendLine($"\"{ticket.TicketNumber}\",\"{EscapeCsv(ticket.Title)}\",\"{ticket.Status}\",\"{ticket.Priority}\",\"{ticket.Category}\",\"{ticket.AssignedTo}\",\"{ticket.Company}\",\"{ticket.CreatedAt:yyyy-MM-dd HH:mm:ss}\",\"{ticket.ResolvedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""}\",\"{ticket.ClosedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""}\",\"{ticket.IsSLABreached}\"");
            }

            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"analytics-export-{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        return Ok(new AnalyticsExportDto
        {
            StartDate = start,
            EndDate = end,
            ExportedAt = DateTime.UtcNow,
            TotalRecords = tickets.Count,
            Tickets = tickets
        });
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Replace("\"", "\"\"");
    }
}

// DTOs
public class DashboardDto
{
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int ClosedTickets { get; set; }
    public int TicketsToday { get; set; }
    public int ResolvedToday { get; set; }
    public int TicketsThisWeek { get; set; }
    public int ResolvedThisWeek { get; set; }
    public int TicketsThisMonth { get; set; }
    public int ResolvedThisMonth { get; set; }
    public int UnassignedTickets { get; set; }
    public int SLABreachedTickets { get; set; }
    public double AverageResolutionHours { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public Dictionary<string, int> TicketsByPriority { get; set; } = new();
    public Dictionary<string, int> TicketsByStatus { get; set; } = new();
}

public class TrendDataDto
{
    public DateTime Date { get; set; }
    public int Created { get; set; }
    public int Resolved { get; set; }
}

public class CategoryStatsDto
{
    public int? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int ClosedTickets { get; set; }
}

public class AgentStatsDto
{
    public string AgentId { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public int TotalAssigned { get; set; }
    public int OpenTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int ClosedTickets { get; set; }
}

public class CompanyStatsDto
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int ClosedTickets { get; set; }
}

public class SLAPerformanceDto
{
    public int TotalTickets { get; set; }
    public int WithinSLA { get; set; }
    public int BreachedSLA { get; set; }
    public double ComplianceRate { get; set; }
}

public class TicketAnalyticsDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int ClosedTickets { get; set; }
    public int SLABreachedTickets { get; set; }
    public double AverageResolutionHours { get; set; }
    public double AverageFirstResponseHours { get; set; }
    public Dictionary<string, int> ByPriority { get; set; } = new();
    public Dictionary<string, int> ByCategory { get; set; } = new();
}

public class TeamPerformanceDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalTicketsHandled { get; set; }
    public int TotalResolved { get; set; }
    public int TotalClosed { get; set; }
    public int TotalSLABreached { get; set; }
    public double OverallResolutionRate { get; set; }
    public List<AgentPerformanceDto> AgentPerformance { get; set; } = new();
}

public class AgentPerformanceDto
{
    public string AgentId { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public int TotalAssigned { get; set; }
    public int Resolved { get; set; }
    public int Closed { get; set; }
    public int Open { get; set; }
    public int SLABreached { get; set; }
    public double ResolutionRate { get; set; }
}

public class TicketExportDto
{
    public string TicketNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool IsSLABreached { get; set; }
}

public class AnalyticsExportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime ExportedAt { get; set; }
    public int TotalRecords { get; set; }
    public List<TicketExportDto> Tickets { get; set; } = new();
}
