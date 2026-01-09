using TicketSystem.Domain.Common;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Domain.Entities;

public class TeamMessage : AuditableEntity
{
    public int? TeamId { get; set; }
    public int? GroupChatId { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public MessageType MessageType { get; set; } = MessageType.Text;
    public bool IsEdited { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? ReplyToMessageId { get; set; }

    // Navigation properties
    public Team? Team { get; set; }
    public GroupChat? GroupChat { get; set; }
    public ApplicationUser Sender { get; set; } = null!;
    public TeamMessage? ReplyToMessage { get; set; }
    public ICollection<TeamMessage> Replies { get; set; } = new List<TeamMessage>();
    public ICollection<TeamMessageAttachment> Attachments { get; set; } = new List<TeamMessageAttachment>();
    public ICollection<TeamMessageReaction> Reactions { get; set; } = new List<TeamMessageReaction>();
    public ICollection<TeamMessageEditHistory> EditHistory { get; set; } = new List<TeamMessageEditHistory>();
}
