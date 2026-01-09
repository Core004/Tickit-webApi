using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class KnowledgeBaseArticleTag : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<KnowledgeBaseArticle> Articles { get; set; } = new List<KnowledgeBaseArticle>();
}
