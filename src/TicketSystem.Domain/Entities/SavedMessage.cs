using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class SavedMessage : AuditableEntity
{
    public string UserId { get; set; } = string.Empty;
    public int MessageId { get; set; }
    public string ChatType { get; set; } = string.Empty; // "team", "group", "direct"
    public string ChatId { get; set; } = string.Empty;
    public string ChatName { get; set; } = string.Empty;
    public string? Note { get; set; }

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public TeamMessage Message { get; set; } = null!;
}
