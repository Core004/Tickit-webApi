using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class AuditLog : AuditableEntity
{
    public string? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; } // JSON
    public string? NewValues { get; set; } // JSON
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; }

    // Navigation properties
    public ApplicationUser? User { get; set; }
}
