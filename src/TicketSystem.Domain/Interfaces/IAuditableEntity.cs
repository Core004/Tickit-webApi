namespace TicketSystem.Domain.Interfaces;

public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    string? CreatedById { get; set; }
    DateTime? UpdatedAt { get; set; }
    string? UpdatedById { get; set; }
}
