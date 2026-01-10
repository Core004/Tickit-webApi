using TicketSystem.Domain.Common;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Domain.Entities;

public class ScheduledMessage : AuditableEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public MessageType MessageType { get; set; } = MessageType.Text;
    public DateTime ScheduledAt { get; set; }
    public int? TeamId { get; set; }
    public int? GroupChatId { get; set; }
    public string? TargetUserId { get; set; } // For direct messages
    public bool IsSent { get; set; }
    public DateTime? SentAt { get; set; }
    public bool IsCancelled { get; set; }

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public Team? Team { get; set; }
    public GroupChat? GroupChat { get; set; }
    public ApplicationUser? TargetUser { get; set; }
}
