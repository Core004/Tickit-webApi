using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class GroupChatMember : BaseEntity
{
    public int GroupChatId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public DateTime? LastReadAt { get; set; }
    public bool IsMuted { get; set; }

    // Navigation properties
    public GroupChat GroupChat { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
