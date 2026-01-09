using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

namespace TicketSystem.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class SLAController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<SLAController> _logger;

    public SLAController(IApplicationDbContext context, ILogger<SLAController> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region SLA Rules

    [HttpGet("rules")]
    public async Task<ActionResult<List<SLARuleDto>>> GetSLARules([FromQuery] bool includeInactive = false)
    {
        var query = _context.SLARules
            .Include(r => r.Priority)
            .Include(r => r.Category)
            .AsQueryable();

        if (!includeInactive)
            query = query.Where(r => r.IsActive);

        var rules = await query
            .OrderBy(r => r.Priority != null ? r.Priority.DisplayOrder : 999)
            .Select(r => new SLARuleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                PriorityId = r.PriorityId,
                PriorityName = r.Priority != null ? r.Priority.Name : null,
                CategoryId = r.CategoryId,
                CategoryName = r.Category != null ? r.Category.Name : null,
                ResponseTimeMinutes = r.ResponseTimeMinutes,
                ResolutionTimeMinutes = r.ResolutionTimeMinutes,
                BusinessHoursOnly = r.BusinessHoursOnly,
                IsActive = r.IsActive
            })
            .ToListAsync();

        return Ok(rules);
    }

    [HttpGet("rules/{id}")]
    public async Task<ActionResult<SLARuleDto>> GetSLARule(int id)
    {
        var rule = await _context.SLARules
            .Include(r => r.Priority)
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rule is null)
            return NotFound();

        return Ok(new SLARuleDto
        {
            Id = rule.Id,
            Name = rule.Name,
            Description = rule.Description,
            PriorityId = rule.PriorityId,
            PriorityName = rule.Priority?.Name,
            CategoryId = rule.CategoryId,
            CategoryName = rule.Category?.Name,
            ResponseTimeMinutes = rule.ResponseTimeMinutes,
            ResolutionTimeMinutes = rule.ResolutionTimeMinutes,
            BusinessHoursOnly = rule.BusinessHoursOnly,
            IsActive = rule.IsActive
        });
    }

    [HttpPost("rules")]
    public async Task<ActionResult<int>> CreateSLARule([FromBody] CreateSLARuleRequest request)
    {
        var rule = new SLARule
        {
            Name = request.Name,
            Description = request.Description,
            PriorityId = request.PriorityId,
            CategoryId = request.CategoryId,
            ResponseTimeMinutes = request.ResponseTimeMinutes,
            ResolutionTimeMinutes = request.ResolutionTimeMinutes,
            BusinessHoursOnly = request.BusinessHoursOnly,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.SLARules.Add(rule);
        await _context.SaveChangesAsync();

        _logger.LogInformation("SLA Rule {Name} created with ID {Id}", rule.Name, rule.Id);

        return CreatedAtAction(nameof(GetSLARule), new { id = rule.Id }, rule.Id);
    }

    [HttpPut("rules/{id}")]
    public async Task<IActionResult> UpdateSLARule(int id, [FromBody] UpdateSLARuleRequest request)
    {
        var rule = await _context.SLARules.FindAsync(id);
        if (rule is null)
            return NotFound();

        rule.Name = request.Name;
        rule.Description = request.Description;
        rule.PriorityId = request.PriorityId;
        rule.CategoryId = request.CategoryId;
        rule.ResponseTimeMinutes = request.ResponseTimeMinutes;
        rule.ResolutionTimeMinutes = request.ResolutionTimeMinutes;
        rule.BusinessHoursOnly = request.BusinessHoursOnly;
        rule.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("rules/{id}/activate")]
    public async Task<IActionResult> ActivateSLARule(int id)
    {
        var rule = await _context.SLARules.FindAsync(id);
        if (rule is null)
            return NotFound();

        rule.IsActive = true;
        rule.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("rules/{id}/deactivate")]
    public async Task<IActionResult> DeactivateSLARule(int id)
    {
        var rule = await _context.SLARules.FindAsync(id);
        if (rule is null)
            return NotFound();

        rule.IsActive = false;
        rule.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("rules/{id}")]
    public async Task<IActionResult> DeleteSLARule(int id)
    {
        var rule = await _context.SLARules.FindAsync(id);
        if (rule is null)
            return NotFound();

        _context.SLARules.Remove(rule);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    #endregion

    #region Business Hours

    [HttpGet("business-hours")]
    public async Task<ActionResult<List<BusinessHoursDto>>> GetBusinessHours([FromQuery] bool includeInactive = false)
    {
        var query = _context.BusinessHours.AsQueryable();

        if (!includeInactive)
            query = query.Where(b => b.IsActive);

        var businessHours = await query
            .OrderBy(b => b.DayOfWeek)
            .Select(b => new BusinessHoursDto
            {
                Id = b.Id,
                Name = b.Name,
                DayOfWeek = b.DayOfWeek,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                TimeZone = b.TimeZone,
                IsActive = b.IsActive
            })
            .ToListAsync();

        return Ok(businessHours);
    }

    [HttpGet("business-hours/{id}")]
    public async Task<ActionResult<BusinessHoursDto>> GetBusinessHoursById(int id)
    {
        var businessHours = await _context.BusinessHours.FindAsync(id);
        if (businessHours is null)
            return NotFound();

        return Ok(new BusinessHoursDto
        {
            Id = businessHours.Id,
            Name = businessHours.Name,
            DayOfWeek = businessHours.DayOfWeek,
            StartTime = businessHours.StartTime,
            EndTime = businessHours.EndTime,
            TimeZone = businessHours.TimeZone,
            IsActive = businessHours.IsActive
        });
    }

    [HttpPost("business-hours")]
    public async Task<ActionResult<int>> CreateBusinessHours([FromBody] CreateBusinessHoursRequest request)
    {
        var businessHours = new BusinessHours
        {
            Name = request.Name,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            TimeZone = request.TimeZone,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.BusinessHours.Add(businessHours);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Business Hours {Name} created with ID {Id}", businessHours.Name, businessHours.Id);

        return CreatedAtAction(nameof(GetBusinessHoursById), new { id = businessHours.Id }, businessHours.Id);
    }

    [HttpPut("business-hours/{id}")]
    public async Task<IActionResult> UpdateBusinessHours(int id, [FromBody] UpdateBusinessHoursRequest request)
    {
        var businessHours = await _context.BusinessHours.FindAsync(id);
        if (businessHours is null)
            return NotFound();

        businessHours.Name = request.Name;
        businessHours.DayOfWeek = request.DayOfWeek;
        businessHours.StartTime = request.StartTime;
        businessHours.EndTime = request.EndTime;
        businessHours.TimeZone = request.TimeZone;
        businessHours.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("business-hours/{id}/activate")]
    public async Task<IActionResult> ActivateBusinessHours(int id)
    {
        var businessHours = await _context.BusinessHours.FindAsync(id);
        if (businessHours is null)
            return NotFound();

        businessHours.IsActive = true;
        businessHours.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("business-hours/{id}/deactivate")]
    public async Task<IActionResult> DeactivateBusinessHours(int id)
    {
        var businessHours = await _context.BusinessHours.FindAsync(id);
        if (businessHours is null)
            return NotFound();

        businessHours.IsActive = false;
        businessHours.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("business-hours/{id}")]
    public async Task<IActionResult> DeleteBusinessHours(int id)
    {
        var businessHours = await _context.BusinessHours.FindAsync(id);
        if (businessHours is null)
            return NotFound();

        _context.BusinessHours.Remove(businessHours);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    #endregion

    #region Holidays

    [HttpGet("holidays")]
    public async Task<ActionResult<List<HolidayDto>>> GetHolidays([FromQuery] int? year = null)
    {
        var query = _context.Holidays.AsQueryable();

        if (year.HasValue)
            query = query.Where(h => h.Date.Year == year.Value);

        var holidays = await query
            .OrderBy(h => h.Date)
            .Select(h => new HolidayDto
            {
                Id = h.Id,
                Name = h.Name,
                Date = h.Date,
                IsRecurring = h.IsRecurring
            })
            .ToListAsync();

        return Ok(holidays);
    }

    [HttpGet("holidays/{id}")]
    public async Task<ActionResult<HolidayDto>> GetHoliday(int id)
    {
        var holiday = await _context.Holidays.FindAsync(id);
        if (holiday is null)
            return NotFound();

        return Ok(new HolidayDto
        {
            Id = holiday.Id,
            Name = holiday.Name,
            Date = holiday.Date,
            IsRecurring = holiday.IsRecurring
        });
    }

    [HttpPost("holidays")]
    public async Task<ActionResult<int>> CreateHoliday([FromBody] CreateHolidayRequest request)
    {
        var holiday = new Holiday
        {
            Name = request.Name,
            Date = request.Date,
            IsRecurring = request.IsRecurring,
            CreatedAt = DateTime.UtcNow
        };

        _context.Holidays.Add(holiday);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Holiday {Name} created with ID {Id}", holiday.Name, holiday.Id);

        return CreatedAtAction(nameof(GetHoliday), new { id = holiday.Id }, holiday.Id);
    }

    [HttpPut("holidays/{id}")]
    public async Task<IActionResult> UpdateHoliday(int id, [FromBody] UpdateHolidayRequest request)
    {
        var holiday = await _context.Holidays.FindAsync(id);
        if (holiday is null)
            return NotFound();

        holiday.Name = request.Name;
        holiday.Date = request.Date;
        holiday.IsRecurring = request.IsRecurring;
        holiday.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("holidays/{id}")]
    public async Task<IActionResult> DeleteHoliday(int id)
    {
        var holiday = await _context.Holidays.FindAsync(id);
        if (holiday is null)
            return NotFound();

        _context.Holidays.Remove(holiday);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    #endregion

    #region Escalation Rules

    [HttpGet("escalations")]
    public async Task<ActionResult<List<EscalationRuleDto>>> GetEscalationRules([FromQuery] bool includeInactive = false)
    {
        var query = _context.EscalationRules
            .Include(e => e.SLARule)
            .AsQueryable();

        if (!includeInactive)
            query = query.Where(e => e.IsActive);

        var rules = await query
            .OrderBy(e => e.TriggerMinutes)
            .Select(e => new EscalationRuleDto
            {
                Id = e.Id,
                Name = e.Name,
                SLARuleId = e.SLARuleId,
                SLARuleName = e.SLARule != null ? e.SLARule.Name : null,
                TriggerMinutes = e.TriggerMinutes,
                Action = e.Action,
                NotifyUserIds = e.NotifyUserIds,
                NotifyRoleIds = e.NotifyRoleIds,
                EmailTemplate = e.EmailTemplate,
                IsActive = e.IsActive
            })
            .ToListAsync();

        return Ok(rules);
    }

    [HttpGet("escalations/{id}")]
    public async Task<ActionResult<EscalationRuleDto>> GetEscalationRule(int id)
    {
        var rule = await _context.EscalationRules
            .Include(e => e.SLARule)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (rule is null)
            return NotFound();

        return Ok(new EscalationRuleDto
        {
            Id = rule.Id,
            Name = rule.Name,
            SLARuleId = rule.SLARuleId,
            SLARuleName = rule.SLARule?.Name,
            TriggerMinutes = rule.TriggerMinutes,
            Action = rule.Action,
            NotifyUserIds = rule.NotifyUserIds,
            NotifyRoleIds = rule.NotifyRoleIds,
            EmailTemplate = rule.EmailTemplate,
            IsActive = rule.IsActive
        });
    }

    [HttpPost("escalations")]
    public async Task<ActionResult<int>> CreateEscalationRule([FromBody] CreateEscalationRuleRequest request)
    {
        var rule = new EscalationRule
        {
            Name = request.Name,
            SLARuleId = request.SLARuleId,
            TriggerMinutes = request.TriggerMinutes,
            Action = request.Action,
            NotifyUserIds = request.NotifyUserIds,
            NotifyRoleIds = request.NotifyRoleIds,
            EmailTemplate = request.EmailTemplate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.EscalationRules.Add(rule);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Escalation Rule {Name} created with ID {Id}", rule.Name, rule.Id);

        return CreatedAtAction(nameof(GetEscalationRule), new { id = rule.Id }, rule.Id);
    }

    [HttpPut("escalations/{id}")]
    public async Task<IActionResult> UpdateEscalationRule(int id, [FromBody] UpdateEscalationRuleRequest request)
    {
        var rule = await _context.EscalationRules.FindAsync(id);
        if (rule is null)
            return NotFound();

        rule.Name = request.Name;
        rule.SLARuleId = request.SLARuleId;
        rule.TriggerMinutes = request.TriggerMinutes;
        rule.Action = request.Action;
        rule.NotifyUserIds = request.NotifyUserIds;
        rule.NotifyRoleIds = request.NotifyRoleIds;
        rule.EmailTemplate = request.EmailTemplate;
        rule.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("escalations/{id}/activate")]
    public async Task<IActionResult> ActivateEscalationRule(int id)
    {
        var rule = await _context.EscalationRules.FindAsync(id);
        if (rule is null)
            return NotFound();

        rule.IsActive = true;
        rule.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("escalations/{id}/deactivate")]
    public async Task<IActionResult> DeactivateEscalationRule(int id)
    {
        var rule = await _context.EscalationRules.FindAsync(id);
        if (rule is null)
            return NotFound();

        rule.IsActive = false;
        rule.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("escalations/{id}")]
    public async Task<IActionResult> DeleteEscalationRule(int id)
    {
        var rule = await _context.EscalationRules.FindAsync(id);
        if (rule is null)
            return NotFound();

        _context.EscalationRules.Remove(rule);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    #endregion
}

// DTOs
public class SLARuleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? PriorityId { get; set; }
    public string? PriorityName { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int ResponseTimeMinutes { get; set; }
    public int ResolutionTimeMinutes { get; set; }
    public bool BusinessHoursOnly { get; set; }
    public bool IsActive { get; set; }
}

public record CreateSLARuleRequest(
    string Name,
    string? Description,
    int? PriorityId,
    int? CategoryId,
    int ResponseTimeMinutes,
    int ResolutionTimeMinutes,
    bool BusinessHoursOnly = true);

public record UpdateSLARuleRequest(
    string Name,
    string? Description,
    int? PriorityId,
    int? CategoryId,
    int ResponseTimeMinutes,
    int ResolutionTimeMinutes,
    bool BusinessHoursOnly = true);

public class BusinessHoursDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string TimeZone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public record CreateBusinessHoursRequest(
    string Name,
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string TimeZone = "UTC");

public record UpdateBusinessHoursRequest(
    string Name,
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string TimeZone = "UTC");

public class HolidayDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public bool IsRecurring { get; set; }
}

public record CreateHolidayRequest(
    string Name,
    DateTime Date,
    bool IsRecurring = false);

public record UpdateHolidayRequest(
    string Name,
    DateTime Date,
    bool IsRecurring = false);

public class EscalationRuleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? SLARuleId { get; set; }
    public string? SLARuleName { get; set; }
    public int TriggerMinutes { get; set; }
    public EscalationAction Action { get; set; }
    public string? NotifyUserIds { get; set; }
    public string? NotifyRoleIds { get; set; }
    public string? EmailTemplate { get; set; }
    public bool IsActive { get; set; }
}

public record CreateEscalationRuleRequest(
    string Name,
    int? SLARuleId,
    int TriggerMinutes,
    EscalationAction Action,
    string? NotifyUserIds,
    string? NotifyRoleIds,
    string? EmailTemplate);

public record UpdateEscalationRuleRequest(
    string Name,
    int? SLARuleId,
    int TriggerMinutes,
    EscalationAction Action,
    string? NotifyUserIds,
    string? NotifyRoleIds,
    string? EmailTemplate);
