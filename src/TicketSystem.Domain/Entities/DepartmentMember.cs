using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class DepartmentMember : BaseEntity
{
    public int DepartmentId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public bool IsManager { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Department Department { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
