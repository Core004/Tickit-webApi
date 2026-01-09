using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class TeamMessageEditHistory : BaseEntity
{
    public int MessageId { get; set; }
    public string PreviousContent { get; set; } = string.Empty;
    public string EditedById { get; set; } = string.Empty;
    public DateTime EditedAt { get; set; }

    // Navigation properties
    public TeamMessage Message { get; set; } = null!;
    public ApplicationUser EditedBy { get; set; } = null!;
}
