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
public class ChatbotController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ChatbotController> _logger;

    public ChatbotController(IApplicationDbContext context, ILogger<ChatbotController> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Chat Sessions

    [HttpGet("sessions")]
    public async Task<ActionResult<List<ChatSessionDto>>> GetMySessions()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var sessions = await _context.ChatSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.StartedAt)
            .Select(s => new ChatSessionDto
            {
                Id = s.Id,
                Title = s.Title,
                IsActive = s.IsActive,
                StartedAt = s.StartedAt,
                EndedAt = s.EndedAt,
                MessageCount = s.Messages.Count
            })
            .ToListAsync();

        return Ok(sessions);
    }

    [HttpGet("sessions/{id}")]
    public async Task<ActionResult<ChatSessionDetailDto>> GetSession(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var session = await _context.ChatSessions
            .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (session is null)
            return NotFound();

        return Ok(new ChatSessionDetailDto
        {
            Id = session.Id,
            Title = session.Title,
            IsActive = session.IsActive,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            Messages = session.Messages.Select(m => new ChatMessageDto
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content,
                CreatedAt = m.CreatedAt,
                TokensUsed = m.TokensUsed
            }).ToList()
        });
    }

    [HttpPost("sessions")]
    public async Task<ActionResult<ChatSessionDto>> StartSession([FromBody] StartSessionRequest? request = null)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var session = new ChatSession
        {
            UserId = userId,
            SessionToken = Guid.NewGuid().ToString(),
            Title = request?.Title ?? "New Chat",
            StartedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ChatSessions.Add(session);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Chat session started with ID {Id} for user {UserId}", session.Id, userId);

        return CreatedAtAction(nameof(GetSession), new { id = session.Id }, new ChatSessionDto
        {
            Id = session.Id,
            Title = session.Title,
            IsActive = session.IsActive,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            MessageCount = 0
        });
    }

    [HttpPut("sessions/{id}/title")]
    public async Task<IActionResult> UpdateSessionTitle(int id, [FromBody] UpdateSessionTitleRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var session = await _context.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (session is null)
            return NotFound();

        session.Title = request.Title;
        session.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("sessions/{id}/end")]
    public async Task<IActionResult> EndSession(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var session = await _context.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (session is null)
            return NotFound();

        session.IsActive = false;
        session.EndedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("sessions/{id}")]
    public async Task<IActionResult> DeleteSession(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var session = await _context.ChatSessions
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (session is null)
            return NotFound();

        // Remove all messages first
        _context.ChatMessages.RemoveRange(session.Messages);
        _context.ChatSessions.Remove(session);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    #endregion

    #region Chat Messages

    [HttpPost("sessions/{sessionId}/messages")]
    public async Task<ActionResult<ChatMessageDto>> SendMessage(int sessionId, [FromBody] ChatbotSendMessageRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var session = await _context.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

        if (session is null)
            return NotFound(new { Message = "Session not found" });

        if (!session.IsActive)
            return BadRequest(new { Message = "Session is no longer active" });

        // Add user message
        var userMessage = new ChatMessage
        {
            SessionId = sessionId,
            Role = MessageRole.User,
            Content = request.Message,
            CreatedAt = DateTime.UtcNow
        };

        _context.ChatMessages.Add(userMessage);
        await _context.SaveChangesAsync();

        // Generate AI response (placeholder - in real implementation, this would call an AI service)
        var aiResponse = await GenerateAIResponse(request.Message);

        var assistantMessage = new ChatMessage
        {
            SessionId = sessionId,
            Role = MessageRole.Assistant,
            Content = aiResponse.Content,
            CreatedAt = DateTime.UtcNow,
            TokensUsed = aiResponse.TokensUsed
        };

        _context.ChatMessages.Add(assistantMessage);

        // Update session title if this is the first message
        var messageCount = await _context.ChatMessages.CountAsync(m => m.SessionId == sessionId);
        if (messageCount <= 2 && session.Title == "New Chat")
        {
            session.Title = request.Message.Length > 50
                ? request.Message[..47] + "..."
                : request.Message;
        }

        session.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new ChatMessageDto
        {
            Id = assistantMessage.Id,
            Role = assistantMessage.Role,
            Content = assistantMessage.Content,
            CreatedAt = assistantMessage.CreatedAt,
            TokensUsed = assistantMessage.TokensUsed
        });
    }

    [HttpGet("sessions/{sessionId}/messages")]
    public async Task<ActionResult<List<ChatMessageDto>>> GetSessionMessages(int sessionId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var session = await _context.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

        if (session is null)
            return NotFound();

        var messages = await _context.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content,
                CreatedAt = m.CreatedAt,
                TokensUsed = m.TokensUsed
            })
            .ToListAsync();

        return Ok(messages);
    }

    #endregion

    #region Quick Actions (Anonymous)

    [HttpPost("quick")]
    [AllowAnonymous]
    public async Task<ActionResult<QuickChatResponseDto>> QuickChat([FromBody] QuickChatRequest request)
    {
        // Generate AI response without saving to database
        var response = await GenerateAIResponse(request.Message);

        return Ok(new QuickChatResponseDto
        {
            Response = response.Content,
            SuggestedArticles = await GetSuggestedArticles(request.Message)
        });
    }

    #endregion

    #region Analytics

    [HttpGet("analytics")]
    public async Task<ActionResult<ChatAnalyticsDto>> GetChatAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var sessionQuery = _context.ChatSessions.AsQueryable();
        var messageQuery = _context.ChatMessages.AsQueryable();

        if (startDate.HasValue)
        {
            sessionQuery = sessionQuery.Where(s => s.StartedAt >= startDate.Value);
            messageQuery = messageQuery.Where(m => m.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            sessionQuery = sessionQuery.Where(s => s.StartedAt <= endDate.Value);
            messageQuery = messageQuery.Where(m => m.CreatedAt <= endDate.Value);
        }

        var totalSessions = await sessionQuery.CountAsync();
        var activeSessions = await sessionQuery.CountAsync(s => s.IsActive);
        var totalMessages = await messageQuery.CountAsync();
        var totalTokens = await messageQuery.SumAsync(m => m.TokensUsed ?? 0);

        var averageMessagesPerSession = totalSessions > 0
            ? (double)totalMessages / totalSessions
            : 0;

        return Ok(new ChatAnalyticsDto
        {
            TotalSessions = totalSessions,
            ActiveSessions = activeSessions,
            TotalMessages = totalMessages,
            TotalTokensUsed = totalTokens,
            AverageMessagesPerSession = Math.Round(averageMessagesPerSession, 2)
        });
    }

    #endregion

    // Helper methods
    private async Task<(string Content, int TokensUsed)> GenerateAIResponse(string userMessage)
    {
        // Placeholder implementation - in real app, this would call an AI service
        // For now, return a helpful response based on keywords

        var lowerMessage = userMessage.ToLower();
        string response;

        if (lowerMessage.Contains("ticket") || lowerMessage.Contains("issue"))
        {
            response = "I can help you with ticket-related questions. To create a new ticket, go to the Tickets section and click 'New Ticket'. If you need to check on an existing ticket, you can search by ticket number or browse your tickets list.";
        }
        else if (lowerMessage.Contains("password") || lowerMessage.Contains("login"))
        {
            response = "For password-related issues, you can reset your password using the 'Forgot Password' link on the login page. If you're having trouble logging in, please ensure your email is correct and check your spam folder for the verification email.";
        }
        else if (lowerMessage.Contains("help") || lowerMessage.Contains("support"))
        {
            response = "I'm here to help! You can ask me about tickets, account issues, or search our knowledge base. For urgent matters, please create a support ticket with high priority.";
        }
        else
        {
            response = "Thank you for your message. I'm an AI assistant here to help you with common questions. You can ask me about creating tickets, account management, or navigating the system. For complex issues, I recommend creating a support ticket.";
        }

        // Simulate token usage
        var tokensUsed = response.Length / 4; // Rough estimation

        await Task.CompletedTask; // Placeholder for async AI call
        return (response, tokensUsed);
    }

    private async Task<List<SuggestedArticleDto>> GetSuggestedArticles(string query)
    {
        var searchTerm = query.ToLower();

        var articles = await _context.KnowledgeBaseArticles
            .Where(a => a.Status == KnowledgeBaseArticleStatus.Published)
            .Where(a =>
                a.Title.ToLower().Contains(searchTerm) ||
                a.Content.ToLower().Contains(searchTerm))
            .OrderByDescending(a => a.ViewCount)
            .Take(3)
            .Select(a => new SuggestedArticleDto
            {
                Id = a.Id,
                Title = a.Title,
                Slug = a.Slug
            })
            .ToListAsync();

        return articles;
    }
}

// DTOs
public class ChatSessionDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int MessageCount { get; set; }
}

public class ChatSessionDetailDto : ChatSessionDto
{
    public List<ChatMessageDto> Messages { get; set; } = new();
}

public class ChatMessageDto
{
    public int Id { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? TokensUsed { get; set; }
}

public record StartSessionRequest(string? Title = null);

public record UpdateSessionTitleRequest(string Title);

public record ChatbotSendMessageRequest(string Message);

public record QuickChatRequest(string Message);

public class QuickChatResponseDto
{
    public string Response { get; set; } = string.Empty;
    public List<SuggestedArticleDto> SuggestedArticles { get; set; } = new();
}

public class SuggestedArticleDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class ChatAnalyticsDto
{
    public int TotalSessions { get; set; }
    public int ActiveSessions { get; set; }
    public int TotalMessages { get; set; }
    public int TotalTokensUsed { get; set; }
    public double AverageMessagesPerSession { get; set; }
}
