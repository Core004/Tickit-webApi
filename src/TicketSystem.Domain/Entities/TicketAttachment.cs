using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class TicketAttachment : AuditableEntity
{
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }

    // Foreign keys
    public int? TicketId { get; set; }
    public int? CommentId { get; set; }
    public string UploadedById { get; set; } = string.Empty;

    // Navigation properties
    public Ticket? Ticket { get; set; }
    public TicketComment? Comment { get; set; }
    public ApplicationUser UploadedBy { get; set; } = null!;
}
