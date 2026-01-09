using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class GroupChat : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AvatarUrl { get; set; }
    public new string CreatedById { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ApplicationUser CreatedBy { get; set; } = null!;
    public ICollection<GroupChatMember> Members { get; set; } = new List<GroupChatMember>();
    public ICollection<TeamMessage> Messages { get; set; } = new List<TeamMessage>();
}
