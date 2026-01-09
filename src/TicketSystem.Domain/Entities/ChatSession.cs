using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class ChatSession : AuditableEntity
{
    public string? UserId { get; set; }
    public string? SessionToken { get; set; }
    public string Title { get; set; } = "New Chat";
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ApplicationUser? User { get; set; }
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
