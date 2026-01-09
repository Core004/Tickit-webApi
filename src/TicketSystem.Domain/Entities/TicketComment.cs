using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class TicketComment : AuditableEntity
{
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public bool IsSystemGenerated { get; set; }

    // Foreign keys
    public int TicketId { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public int? ParentCommentId { get; set; }

    // Navigation properties
    public Ticket Ticket { get; set; } = null!;
    public ApplicationUser Author { get; set; } = null!;
    public TicketComment? ParentComment { get; set; }
    public ICollection<TicketComment> Replies { get; set; } = new List<TicketComment>();
    public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
}
