using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class ProductVersion : AuditableEntity
{
    public string Version { get; set; } = string.Empty;
    public string? ReleaseNotes { get; set; }
    public DateTime ReleaseDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Foreign keys
    public int ProductId { get; set; }

    // Navigation properties
    public Product Product { get; set; } = null!;
}
