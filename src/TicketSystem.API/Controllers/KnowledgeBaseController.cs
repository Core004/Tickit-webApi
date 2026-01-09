using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

namespace TicketSystem.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class KnowledgeBaseController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<KnowledgeBaseController> _logger;

    public KnowledgeBaseController(IApplicationDbContext context, ILogger<KnowledgeBaseController> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Articles

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PaginatedArticleResponse>> GetArticles(
        [FromQuery] string? search = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] KnowledgeBaseArticleStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.KnowledgeBaseArticles
            .Include(a => a.Author)
            .Include(a => a.Category)
            .AsQueryable();

        // For anonymous users, only show published articles
        if (!User.Identity?.IsAuthenticated ?? true)
            query = query.Where(a => a.Status == KnowledgeBaseArticleStatus.Published);
        else if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(a =>
                a.Title.ToLower().Contains(search) ||
                a.Content.ToLower().Contains(search) ||
                (a.MetaDescription != null && a.MetaDescription.ToLower().Contains(search)));
        }

        if (categoryId.HasValue)
            query = query.Where(a => a.CategoryId == categoryId.Value);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var articles = await query
            .OrderByDescending(a => a.PublishedAt ?? a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ArticleListDto
            {
                Id = a.Id,
                Slug = a.Slug,
                Title = a.Title,
                MetaDescription = a.MetaDescription,
                CategoryId = a.CategoryId,
                CategoryName = a.Category != null ? a.Category.Name : null,
                AuthorId = a.AuthorId,
                AuthorName = a.Author != null ? a.Author.FirstName + " " + a.Author.LastName : null,
                Status = a.Status,
                ViewCount = a.ViewCount,
                IsFeatured = a.IsFeatured,
                PublishedAt = a.PublishedAt,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return Ok(new PaginatedArticleResponse
        {
            Items = articles,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        });
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ArticleDetailDto>> GetArticle(int id)
    {
        var article = await _context.KnowledgeBaseArticles
            .Include(a => a.Author)
            .Include(a => a.Category)
            .Include(a => a.ArticleTags)
            .Include(a => a.Feedbacks)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (article is null)
            return NotFound();

        // Increment view count
        article.ViewCount++;
        await _context.SaveChangesAsync();

        return Ok(new ArticleDetailDto
        {
            Id = article.Id,
            Slug = article.Slug,
            Title = article.Title,
            Content = article.Content,
            MetaDescription = article.MetaDescription,
            CategoryId = article.CategoryId,
            CategoryName = article.Category?.Name,
            AuthorId = article.AuthorId,
            AuthorName = article.Author != null ? article.Author.FirstName + " " + article.Author.LastName : null,
            Status = article.Status,
            ViewCount = article.ViewCount,
            IsFeatured = article.IsFeatured,
            PublishedAt = article.PublishedAt,
            CreatedAt = article.CreatedAt,
            UpdatedAt = article.UpdatedAt,
            Tags = article.ArticleTags.Select(t => new ArticleTagDto
            {
                Id = t.Id,
                Name = t.Name
            }).ToList(),
            FeedbackSummary = new FeedbackSummaryDto
            {
                TotalFeedback = article.Feedbacks.Count,
                HelpfulCount = article.Feedbacks.Count(f => f.IsHelpful),
                NotHelpfulCount = article.Feedbacks.Count(f => !f.IsHelpful)
            }
        });
    }

    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<ArticleDetailDto>> GetArticleBySlug(string slug)
    {
        var article = await _context.KnowledgeBaseArticles
            .Include(a => a.Author)
            .Include(a => a.Category)
            .Include(a => a.ArticleTags)
            .Include(a => a.Feedbacks)
            .FirstOrDefaultAsync(a => a.Slug == slug);

        if (article is null)
            return NotFound();

        // For anonymous users, only show published articles
        if ((!User.Identity?.IsAuthenticated ?? true) && article.Status != KnowledgeBaseArticleStatus.Published)
            return NotFound();

        // Increment view count
        article.ViewCount++;
        await _context.SaveChangesAsync();

        return Ok(new ArticleDetailDto
        {
            Id = article.Id,
            Slug = article.Slug,
            Title = article.Title,
            Content = article.Content,
            MetaDescription = article.MetaDescription,
            CategoryId = article.CategoryId,
            CategoryName = article.Category?.Name,
            AuthorId = article.AuthorId,
            AuthorName = article.Author != null ? article.Author.FirstName + " " + article.Author.LastName : null,
            Status = article.Status,
            ViewCount = article.ViewCount,
            IsFeatured = article.IsFeatured,
            PublishedAt = article.PublishedAt,
            CreatedAt = article.CreatedAt,
            UpdatedAt = article.UpdatedAt,
            Tags = article.ArticleTags.Select(t => new ArticleTagDto
            {
                Id = t.Id,
                Name = t.Name
            }).ToList(),
            FeedbackSummary = new FeedbackSummaryDto
            {
                TotalFeedback = article.Feedbacks.Count,
                HelpfulCount = article.Feedbacks.Count(f => f.IsHelpful),
                NotHelpfulCount = article.Feedbacks.Count(f => !f.IsHelpful)
            }
        });
    }

    [HttpPost]
    public async Task<ActionResult<int>> CreateArticle([FromBody] CreateArticleRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var article = new KnowledgeBaseArticle
        {
            Slug = GenerateSlug(request.Title),
            Title = request.Title,
            Content = request.Content,
            MetaDescription = request.MetaDescription,
            CategoryId = request.CategoryId,
            AuthorId = userId,
            Status = KnowledgeBaseArticleStatus.Draft,
            ViewCount = 0,
            IsFeatured = request.IsFeatured,
            CreatedAt = DateTime.UtcNow
        };

        _context.KnowledgeBaseArticles.Add(article);
        await _context.SaveChangesAsync();

        // Add tags if provided
        if (request.TagIds?.Any() == true)
        {
            foreach (var tagId in request.TagIds)
            {
                var tag = await _context.KnowledgeBaseArticleTags.FindAsync(tagId);
                if (tag != null)
                {
                    article.ArticleTags.Add(tag);
                }
            }
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Article {Title} created with ID {Id}", article.Title, article.Id);

        return CreatedAtAction(nameof(GetArticle), new { id = article.Id }, article.Id);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateArticle(int id, [FromBody] UpdateArticleRequest request)
    {
        var article = await _context.KnowledgeBaseArticles
            .Include(a => a.ArticleTags)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (article is null)
            return NotFound();

        article.Title = request.Title;
        article.Slug = GenerateSlug(request.Title);
        article.Content = request.Content;
        article.MetaDescription = request.MetaDescription;
        article.CategoryId = request.CategoryId;
        article.IsFeatured = request.IsFeatured;
        article.UpdatedAt = DateTime.UtcNow;

        // Update tags
        article.ArticleTags.Clear();
        if (request.TagIds?.Any() == true)
        {
            foreach (var tagId in request.TagIds)
            {
                var tag = await _context.KnowledgeBaseArticleTags.FindAsync(tagId);
                if (tag != null)
                {
                    article.ArticleTags.Add(tag);
                }
            }
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/publish")]
    public async Task<IActionResult> PublishArticle(int id)
    {
        var article = await _context.KnowledgeBaseArticles.FindAsync(id);
        if (article is null)
            return NotFound();

        article.Status = KnowledgeBaseArticleStatus.Published;
        article.PublishedAt = DateTime.UtcNow;
        article.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/unpublish")]
    public async Task<IActionResult> UnpublishArticle(int id)
    {
        var article = await _context.KnowledgeBaseArticles.FindAsync(id);
        if (article is null)
            return NotFound();

        article.Status = KnowledgeBaseArticleStatus.Draft;
        article.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/archive")]
    public async Task<IActionResult> ArchiveArticle(int id)
    {
        var article = await _context.KnowledgeBaseArticles.FindAsync(id);
        if (article is null)
            return NotFound();

        article.Status = KnowledgeBaseArticleStatus.Archived;
        article.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteArticle(int id)
    {
        var article = await _context.KnowledgeBaseArticles.FindAsync(id);
        if (article is null)
            return NotFound();

        _context.KnowledgeBaseArticles.Remove(article);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    #endregion

    #region Tags

    [HttpGet("tags")]
    public async Task<ActionResult<List<ArticleTagDto>>> GetTags()
    {
        var tags = await _context.KnowledgeBaseArticleTags
            .OrderBy(t => t.Name)
            .Select(t => new ArticleTagDto
            {
                Id = t.Id,
                Name = t.Name
            })
            .ToListAsync();

        return Ok(tags);
    }

    [HttpGet("tags/{id}")]
    public async Task<ActionResult<ArticleTagDto>> GetTag(int id)
    {
        var tag = await _context.KnowledgeBaseArticleTags.FindAsync(id);
        if (tag is null)
            return NotFound();

        return Ok(new ArticleTagDto
        {
            Id = tag.Id,
            Name = tag.Name
        });
    }

    [HttpPost("tags")]
    public async Task<ActionResult<int>> CreateTag([FromBody] CreateTagRequest request)
    {
        var tag = new KnowledgeBaseArticleTag
        {
            Name = request.Name,
            CreatedAt = DateTime.UtcNow
        };

        _context.KnowledgeBaseArticleTags.Add(tag);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tag {Name} created with ID {Id}", tag.Name, tag.Id);

        return CreatedAtAction(nameof(GetTag), new { id = tag.Id }, tag.Id);
    }

    [HttpPut("tags/{id}")]
    public async Task<IActionResult> UpdateTag(int id, [FromBody] UpdateTagRequest request)
    {
        var tag = await _context.KnowledgeBaseArticleTags.FindAsync(id);
        if (tag is null)
            return NotFound();

        tag.Name = request.Name;
        tag.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("tags/{id}")]
    public async Task<IActionResult> DeleteTag(int id)
    {
        var tag = await _context.KnowledgeBaseArticleTags.FindAsync(id);
        if (tag is null)
            return NotFound();

        _context.KnowledgeBaseArticleTags.Remove(tag);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    #endregion

    #region Feedback

    [HttpPost("{articleId}/feedback")]
    [AllowAnonymous]
    public async Task<ActionResult<int>> SubmitFeedback(int articleId, [FromBody] CreateFeedbackRequest request)
    {
        var article = await _context.KnowledgeBaseArticles.FindAsync(articleId);
        if (article is null)
            return NotFound();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var feedback = new KnowledgeBaseArticleFeedback
        {
            ArticleId = articleId,
            UserId = userId,
            IsHelpful = request.IsHelpful,
            Comment = request.Comment,
            CreatedAt = DateTime.UtcNow
        };

        _context.KnowledgeBaseArticleFeedbacks.Add(feedback);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Feedback submitted for article {ArticleId}", articleId);

        return Ok(feedback.Id);
    }

    [HttpGet("{articleId}/feedback")]
    public async Task<ActionResult<List<FeedbackDto>>> GetArticleFeedback(int articleId)
    {
        var feedbacks = await _context.KnowledgeBaseArticleFeedbacks
            .Where(f => f.ArticleId == articleId)
            .Include(f => f.User)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FeedbackDto
            {
                Id = f.Id,
                UserId = f.UserId,
                UserName = f.User != null ? f.User.FirstName + " " + f.User.LastName : "Anonymous",
                IsHelpful = f.IsHelpful,
                Comment = f.Comment,
                CreatedAt = f.CreatedAt
            })
            .ToListAsync();

        return Ok(feedbacks);
    }

    [HttpDelete("feedback/{feedbackId}")]
    public async Task<IActionResult> DeleteFeedback(int feedbackId)
    {
        var feedback = await _context.KnowledgeBaseArticleFeedbacks.FindAsync(feedbackId);
        if (feedback is null)
            return NotFound();

        _context.KnowledgeBaseArticleFeedbacks.Remove(feedback);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    #endregion

    #region Search

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ArticleSearchResultDto>>> SearchArticles([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new List<ArticleSearchResultDto>());

        var searchTerm = q.ToLower();
        var query = _context.KnowledgeBaseArticles
            .Where(a => a.Status == KnowledgeBaseArticleStatus.Published)
            .Where(a =>
                a.Title.ToLower().Contains(searchTerm) ||
                a.Content.ToLower().Contains(searchTerm) ||
                (a.MetaDescription != null && a.MetaDescription.ToLower().Contains(searchTerm)));

        var results = await query
            .OrderByDescending(a => a.ViewCount)
            .Take(20)
            .Select(a => new ArticleSearchResultDto
            {
                Id = a.Id,
                Slug = a.Slug,
                Title = a.Title,
                Excerpt = a.MetaDescription ?? (a.Content.Length > 200 ? a.Content.Substring(0, 200) + "..." : a.Content),
                ViewCount = a.ViewCount
            })
            .ToListAsync();

        return Ok(results);
    }

    #endregion

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLower()
            .Replace(" ", "-")
            .Replace("&", "and");

        // Remove special characters
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");

        // Remove duplicate hyphens
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");

        // Trim hyphens from ends
        slug = slug.Trim('-');

        return slug + "-" + DateTime.UtcNow.Ticks.ToString()[^6..];
    }
}

// DTOs
public class ArticleListDto
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? MetaDescription { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public KnowledgeBaseArticleStatus Status { get; set; }
    public int ViewCount { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ArticleDetailDto : ArticleListDto
{
    public string Content { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public List<ArticleTagDto> Tags { get; set; } = new();
    public FeedbackSummaryDto FeedbackSummary { get; set; } = new();
}

public class ArticleSearchResultDto
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    public int ViewCount { get; set; }
}

public class ArticleTagDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class FeedbackSummaryDto
{
    public int TotalFeedback { get; set; }
    public int HelpfulCount { get; set; }
    public int NotHelpfulCount { get; set; }
}

public class FeedbackDto
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public bool IsHelpful { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PaginatedArticleResponse
{
    public List<ArticleListDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public record CreateArticleRequest(
    string Title,
    string Content,
    string? MetaDescription,
    int? CategoryId,
    bool IsFeatured = false,
    List<int>? TagIds = null);

public record UpdateArticleRequest(
    string Title,
    string Content,
    string? MetaDescription,
    int? CategoryId,
    bool IsFeatured = false,
    List<int>? TagIds = null);

public record CreateTagRequest(string Name);

public record UpdateTagRequest(string Name);

public record CreateFeedbackRequest(
    bool IsHelpful,
    string? Comment = null);
