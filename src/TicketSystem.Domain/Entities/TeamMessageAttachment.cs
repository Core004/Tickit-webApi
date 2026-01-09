using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class TeamMessageAttachment : AuditableEntity
{
    public int MessageId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }

    // Navigation properties
    public TeamMessage Message { get; set; } = null!;
}
