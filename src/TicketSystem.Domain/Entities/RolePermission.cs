using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class RolePermission : BaseEntity
{
    public string RoleId { get; set; } = string.Empty;
    public int PermissionId { get; set; }

    // Navigation properties
    public Permission Permission { get; set; } = null!;
}
