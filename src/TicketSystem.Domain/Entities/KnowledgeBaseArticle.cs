using TicketSystem.Domain.Common;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Domain.Entities;

public class KnowledgeBaseArticle : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? MetaDescription { get; set; }
    public KnowledgeBaseArticleStatus Status { get; set; } = KnowledgeBaseArticleStatus.Draft;
    public int ViewCount { get; set; }
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }
    public DateTime? PublishedAt { get; set; }

    // Foreign keys
    public int? CategoryId { get; set; }
    public string? AuthorId { get; set; }

    // Navigation properties
    public TicketCategory? Category { get; set; }
    public ApplicationUser? Author { get; set; }
    public ICollection<KnowledgeBaseArticleTag> ArticleTags { get; set; } = new List<KnowledgeBaseArticleTag>();
    public ICollection<KnowledgeBaseArticleFeedback> Feedbacks { get; set; } = new List<KnowledgeBaseArticleFeedback>();
}
