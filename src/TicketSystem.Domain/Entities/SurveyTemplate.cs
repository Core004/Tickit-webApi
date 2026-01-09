using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class SurveyTemplate : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Questions { get; set; } = "[]"; // JSON array of questions
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public bool SendAutomatically { get; set; } = true;
    public int? SendAfterHours { get; set; } // Hours after ticket resolution

    // Navigation properties
    public ICollection<TicketSurvey> TicketSurveys { get; set; } = new List<TicketSurvey>();
}
