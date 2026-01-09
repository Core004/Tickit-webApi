namespace TicketSystem.Domain.Common;

public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedById { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedById { get; set; }
}
