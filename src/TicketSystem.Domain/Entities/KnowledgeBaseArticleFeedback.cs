using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class KnowledgeBaseArticleFeedback : BaseEntity
{
    public int ArticleId { get; set; }
    public string? UserId { get; set; }
    public bool IsHelpful { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public KnowledgeBaseArticle Article { get; set; } = null!;
    public ApplicationUser? User { get; set; }
}
