using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class TeamMessageReaction : BaseEntity
{
    public int MessageId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public TeamMessage Message { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
