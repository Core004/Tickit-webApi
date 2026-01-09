using TicketSystem.Domain.Common;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Domain.Entities;

public class ChatMessage : BaseEntity
{
    public int SessionId { get; set; }
    public MessageRole Role { get; set; } // User or Assistant
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? TokensUsed { get; set; }

    // Navigation properties
    public ChatSession Session { get; set; } = null!;
}
