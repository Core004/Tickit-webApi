using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class TicketSurvey : AuditableEntity
{
    public int TicketId { get; set; }
    public int SurveyTemplateId { get; set; }
    public string? UserId { get; set; }
    public string Responses { get; set; } = "{}"; // JSON object of responses
    public int? OverallRating { get; set; } // 1-5 rating
    public string? Comments { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsCompleted { get; set; }

    // Navigation properties
    public Ticket Ticket { get; set; } = null!;
    public SurveyTemplate SurveyTemplate { get; set; } = null!;
    public ApplicationUser? User { get; set; }
}
