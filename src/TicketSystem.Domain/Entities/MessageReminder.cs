using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class MessageReminder : AuditableEntity
{
    public string UserId { get; set; } = string.Empty;
    public int MessageId { get; set; }
    public string ChatType { get; set; } = string.Empty; // "team", "group", "direct"
    public string ChatId { get; set; } = string.Empty;
    public string ChatName { get; set; } = string.Empty;
    public DateTime RemindAt { get; set; }
    public string? Note { get; set; }
    public bool IsTriggered { get; set; }
    public DateTime? TriggeredAt { get; set; }
    public bool IsCancelled { get; set; }

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public TeamMessage Message { get; set; } = null!;
}
