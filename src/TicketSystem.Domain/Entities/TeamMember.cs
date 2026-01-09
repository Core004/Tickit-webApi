using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class TeamMember : BaseEntity
{
    public int TeamId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public bool IsLead { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Team Team { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
