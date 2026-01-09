using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class Notification : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Link { get; set; }
    public string Type { get; set; } = "info";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
}
