using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class UserPermission : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public int PermissionId { get; set; }
    public bool IsGranted { get; set; } = true;

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
