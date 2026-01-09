using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Domain.Entities;

namespace TicketSystem.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class SurveysController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<SurveysController> _logger;

    public SurveysController(IApplicationDbContext context, ILogger<SurveysController> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Survey Templates

    [HttpGet("templates")]
    public async Task<ActionResult<List<SurveyTemplateDto>>> GetSurveyTemplates([FromQuery] bool includeInactive = false)
    {
        var query = _context.SurveyTemplates.AsQueryable();

        if (!includeInactive)
            query = query.Where(t => t.IsActive);

        var templates = await query
            .OrderBy(t => t.Name)
            .Select(t => new SurveyTemplateDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Questions = t.Questions,
                IsDefault = t.IsDefault,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return Ok(templates);
    }

    [HttpGet("templates/{id}")]
    public async Task<ActionResult<SurveyTemplateDto>> GetSurveyTemplate(int id)
    {
        var template = await _context.SurveyTemplates.FindAsync(id);
        if (template is null)
            return NotFound();

        return Ok(new SurveyTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Questions = template.Questions,
            IsDefault = template.IsDefault,
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        });
    }

    [HttpPost("templates")]
    public async Task<ActionResult<int>> CreateSurveyTemplate([FromBody] CreateSurveyTemplateRequest request)
    {
        // If this is set as default, remove default from others
        if (request.IsDefault)
        {
            var existingDefault = await _context.SurveyTemplates
                .Where(t => t.IsDefault)
                .ToListAsync();
            foreach (var t in existingDefault)
            {
                t.IsDefault = false;
            }
        }

        var template = new SurveyTemplate
        {
            Name = request.Name,
            Description = request.Description,
            Questions = request.Questions,
            IsDefault = request.IsDefault,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.SurveyTemplates.Add(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Survey Template {Name} created with ID {Id}", template.Name, template.Id);

        return CreatedAtAction(nameof(GetSurveyTemplate), new { id = template.Id }, template.Id);
    }

    [HttpPut("templates/{id}")]
    public async Task<IActionResult> UpdateSurveyTemplate(int id, [FromBody] UpdateSurveyTemplateRequest request)
    {
        var template = await _context.SurveyTemplates.FindAsync(id);
        if (template is null)
            return NotFound();

        // If this is set as default, remove default from others
        if (request.IsDefault && !template.IsDefault)
        {
            var existingDefault = await _context.SurveyTemplates
                .Where(t => t.IsDefault && t.Id != id)
                .ToListAsync();
            foreach (var t in existingDefault)
            {
                t.IsDefault = false;
            }
        }

        template.Name = request.Name;
        template.Description = request.Description;
        template.Questions = request.Questions;
        template.IsDefault = request.IsDefault;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("templates/{id}/activate")]
    public async Task<IActionResult> ActivateSurveyTemplate(int id)
    {
        var template = await _context.SurveyTemplates.FindAsync(id);
        if (template is null)
            return NotFound();

        template.IsActive = true;
        template.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("templates/{id}/deactivate")]
    public async Task<IActionResult> DeactivateSurveyTemplate(int id)
    {
        var template = await _context.SurveyTemplates.FindAsync(id);
        if (template is null)
            return NotFound();

        template.IsActive = false;
        template.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("templates/{id}/set-default")]
    public async Task<IActionResult> SetDefaultSurveyTemplate(int id)
    {
        var template = await _context.SurveyTemplates.FindAsync(id);
        if (template is null)
            return NotFound();

        // Remove default from all other templates
        var existingDefault = await _context.SurveyTemplates
            .Where(t => t.IsDefault)
            .ToListAsync();
        foreach (var t in existingDefault)
        {
            t.IsDefault = false;
        }

        template.IsDefault = true;
        template.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("templates/{id}")]
    public async Task<IActionResult> DeleteSurveyTemplate(int id)
    {
        var template = await _context.SurveyTemplates.FindAsync(id);
        if (template is null)
            return NotFound();

        // Check if template has surveys
        var hasSurveys = await _context.TicketSurveys.AnyAsync(s => s.SurveyTemplateId == id);
        if (hasSurveys)
            return BadRequest(new { Message = "Cannot delete template with existing surveys" });

        _context.SurveyTemplates.Remove(template);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    #endregion

    #region Ticket Surveys

    [HttpGet]
    public async Task<ActionResult<PaginatedTicketSurveyResponse>> GetTicketSurveys(
        [FromQuery] int? ticketId = null,
        [FromQuery] bool? isCompleted = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.TicketSurveys
            .Include(s => s.Ticket)
            .Include(s => s.SurveyTemplate)
            .Include(s => s.User)
            .AsQueryable();

        if (ticketId.HasValue)
            query = query.Where(s => s.TicketId == ticketId.Value);

        if (isCompleted.HasValue)
            query = query.Where(s => s.IsCompleted == isCompleted.Value);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var surveys = await query
            .OrderByDescending(s => s.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new TicketSurveyDto
            {
                Id = s.Id,
                TicketId = s.TicketId,
                TicketNumber = s.Ticket.TicketNumber,
                TicketTitle = s.Ticket.Title,
                SurveyTemplateId = s.SurveyTemplateId,
                SurveyTemplateName = s.SurveyTemplate.Name,
                UserId = s.UserId,
                UserName = s.User != null ? s.User.FirstName + " " + s.User.LastName : null,
                OverallRating = s.OverallRating,
                Comments = s.Comments,
                IsCompleted = s.IsCompleted,
                SentAt = s.SentAt,
                CompletedAt = s.CompletedAt
            })
            .ToListAsync();

        return Ok(new PaginatedTicketSurveyResponse
        {
            Items = surveys,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TicketSurveyDetailDto>> GetTicketSurvey(int id)
    {
        var survey = await _context.TicketSurveys
            .Include(s => s.Ticket)
            .Include(s => s.SurveyTemplate)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (survey is null)
            return NotFound();

        return Ok(new TicketSurveyDetailDto
        {
            Id = survey.Id,
            TicketId = survey.TicketId,
            TicketNumber = survey.Ticket.TicketNumber,
            TicketTitle = survey.Ticket.Title,
            SurveyTemplateId = survey.SurveyTemplateId,
            SurveyTemplateName = survey.SurveyTemplate.Name,
            Questions = survey.SurveyTemplate.Questions,
            UserId = survey.UserId,
            UserName = survey.User != null ? survey.User.FirstName + " " + survey.User.LastName : null,
            Responses = survey.Responses,
            OverallRating = survey.OverallRating,
            Comments = survey.Comments,
            IsCompleted = survey.IsCompleted,
            SentAt = survey.SentAt,
            CompletedAt = survey.CompletedAt
        });
    }

    [HttpPost]
    public async Task<ActionResult<int>> SendSurvey([FromBody] SendSurveyRequest request)
    {
        var ticket = await _context.Tickets.FindAsync(request.TicketId);
        if (ticket is null)
            return NotFound(new { Message = "Ticket not found" });

        var template = await _context.SurveyTemplates.FindAsync(request.SurveyTemplateId);
        if (template is null)
            return NotFound(new { Message = "Survey template not found" });

        var survey = new TicketSurvey
        {
            TicketId = request.TicketId,
            SurveyTemplateId = request.SurveyTemplateId,
            UserId = request.UserId,
            Responses = "{}",
            IsCompleted = false,
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.TicketSurveys.Add(survey);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Survey sent for ticket {TicketId} with ID {Id}", request.TicketId, survey.Id);

        return CreatedAtAction(nameof(GetTicketSurvey), new { id = survey.Id }, survey.Id);
    }

    [HttpPost("{id}/submit")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitSurveyResponse(int id, [FromBody] SubmitSurveyRequest request)
    {
        var survey = await _context.TicketSurveys.FindAsync(id);
        if (survey is null)
            return NotFound();

        if (survey.IsCompleted)
            return BadRequest(new { Message = "Survey has already been completed" });

        survey.Responses = request.Responses;
        survey.OverallRating = request.OverallRating;
        survey.Comments = request.Comments;
        survey.IsCompleted = true;
        survey.CompletedAt = DateTime.UtcNow;
        survey.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Survey {Id} completed with rating {Rating}", id, request.OverallRating);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTicketSurvey(int id)
    {
        var survey = await _context.TicketSurveys.FindAsync(id);
        if (survey is null)
            return NotFound();

        _context.TicketSurveys.Remove(survey);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    #endregion

    #region Analytics

    [HttpGet("analytics")]
    public async Task<ActionResult<SurveyAnalyticsDto>> GetSurveyAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var query = _context.TicketSurveys
            .Where(s => s.IsCompleted)
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(s => s.CompletedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(s => s.CompletedAt <= endDate.Value);

        var surveys = await query.ToListAsync();

        var totalSurveys = surveys.Count;
        var averageRating = totalSurveys > 0
            ? surveys.Where(s => s.OverallRating.HasValue).Average(s => s.OverallRating!.Value)
            : 0;

        var ratingDistribution = surveys
            .Where(s => s.OverallRating.HasValue)
            .GroupBy(s => s.OverallRating!.Value)
            .Select(g => new RatingDistributionItem
            {
                Rating = g.Key,
                Count = g.Count()
            })
            .OrderBy(r => r.Rating)
            .ToList();

        var pendingSurveys = await _context.TicketSurveys
            .CountAsync(s => !s.IsCompleted);

        return Ok(new SurveyAnalyticsDto
        {
            TotalCompletedSurveys = totalSurveys,
            AverageRating = Math.Round(averageRating, 2),
            RatingDistribution = ratingDistribution,
            PendingSurveys = pendingSurveys
        });
    }

    #endregion
}

// DTOs
public class SurveyTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Questions { get; set; } = "[]";
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public record CreateSurveyTemplateRequest(
    string Name,
    string? Description,
    string Questions,
    bool IsDefault = false);

public record UpdateSurveyTemplateRequest(
    string Name,
    string? Description,
    string Questions,
    bool IsDefault = false);

public class TicketSurveyDto
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string TicketTitle { get; set; } = string.Empty;
    public int SurveyTemplateId { get; set; }
    public string SurveyTemplateName { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public int? OverallRating { get; set; }
    public string? Comments { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class TicketSurveyDetailDto : TicketSurveyDto
{
    public string Questions { get; set; } = "[]";
    public string Responses { get; set; } = "{}";
}

public class PaginatedTicketSurveyResponse
{
    public List<TicketSurveyDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public record SendSurveyRequest(
    int TicketId,
    int SurveyTemplateId,
    string? UserId = null);

public record SubmitSurveyRequest(
    string Responses,
    int? OverallRating,
    string? Comments = null);

public class SurveyAnalyticsDto
{
    public int TotalCompletedSurveys { get; set; }
    public double AverageRating { get; set; }
    public List<RatingDistributionItem> RatingDistribution { get; set; } = new();
    public int PendingSurveys { get; set; }
}

public class RatingDistributionItem
{
    public int Rating { get; set; }
    public int Count { get; set; }
}
