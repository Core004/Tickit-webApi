using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace TicketSystem.API.Hubs;

[Authorize]
public class ChatbotHub : Hub
{
    private static readonly Dictionary<string, ChatbotSession> _activeSessions = new();
    private static readonly object _lock = new();

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chatbot_{userId}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chatbot_{userId}");
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Start a new chatbot session
    /// </summary>
    public async Task<string> StartSession(string? context = null)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new HubException("User not authenticated");
        }

        var sessionId = Guid.NewGuid().ToString();
        var session = new ChatbotSession
        {
            SessionId = sessionId,
            UserId = userId,
            StartedAt = DateTime.UtcNow,
            Context = context,
            IsActive = true
        };

        lock (_lock)
        {
            _activeSessions[sessionId] = session;
        }

        await Clients.Caller.SendAsync("SessionStarted", new
        {
            SessionId = sessionId,
            StartedAt = session.StartedAt
        });

        return sessionId;
    }

    /// <summary>
    /// End an active chatbot session
    /// </summary>
    public async Task EndSession(string sessionId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        lock (_lock)
        {
            if (_activeSessions.TryGetValue(sessionId, out var session) && session.UserId == userId)
            {
                session.IsActive = false;
                session.EndedAt = DateTime.UtcNow;
            }
        }

        await Clients.Caller.SendAsync("SessionEnded", new
        {
            SessionId = sessionId,
            EndedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Send a message to the chatbot
    /// </summary>
    public async Task SendMessage(string sessionId, string message)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new HubException("User not authenticated");
        }

        ChatbotSession? session;
        lock (_lock)
        {
            _activeSessions.TryGetValue(sessionId, out session);
        }

        if (session == null || session.UserId != userId || !session.IsActive)
        {
            throw new HubException("Invalid or inactive session");
        }

        // Acknowledge message received
        await Clients.Caller.SendAsync("MessageReceived", new
        {
            SessionId = sessionId,
            Message = message,
            Timestamp = DateTime.UtcNow
        });

        // Signal that bot is processing
        await Clients.Caller.SendAsync("BotTyping", new
        {
            SessionId = sessionId,
            IsTyping = true
        });

        // Note: The actual AI processing would be done by the ChatbotService
        // This hub is just for real-time communication
    }

    /// <summary>
    /// Request conversation history
    /// </summary>
    public async Task GetHistory(string sessionId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // This would typically fetch from database via a service
        await Clients.Caller.SendAsync("HistoryReceived", new
        {
            SessionId = sessionId,
            Messages = new List<object>() // Placeholder - would be populated from database
        });
    }

    /// <summary>
    /// Rate a chatbot response
    /// </summary>
    public async Task RateResponse(string sessionId, int messageId, int rating, string? feedback = null)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        await Clients.Caller.SendAsync("RatingReceived", new
        {
            SessionId = sessionId,
            MessageId = messageId,
            Rating = rating,
            Feedback = feedback,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Request escalation to human agent
    /// </summary>
    public async Task RequestEscalation(string sessionId, string? reason = null)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        // Notify the user
        await Clients.Caller.SendAsync("EscalationRequested", new
        {
            SessionId = sessionId,
            Reason = reason,
            Timestamp = DateTime.UtcNow,
            Message = "Your request has been submitted. A support agent will be with you shortly."
        });

        // This would typically create a ticket or notify support agents
        // via the NotificationHub
    }

    /// <summary>
    /// Send feedback about the chatbot experience
    /// </summary>
    public async Task SendFeedback(string sessionId, int overallRating, string? comments = null)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        await Clients.Caller.SendAsync("FeedbackReceived", new
        {
            SessionId = sessionId,
            OverallRating = overallRating,
            Comments = comments,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get suggested responses/quick actions
    /// </summary>
    public async Task GetSuggestions(string sessionId, string currentInput)
    {
        // This would typically use AI to generate suggestions
        await Clients.Caller.SendAsync("SuggestionsReceived", new
        {
            SessionId = sessionId,
            Suggestions = new[]
            {
                "Check ticket status",
                "Create new ticket",
                "Search knowledge base",
                "Contact support"
            }
        });
    }
}

public class ChatbotSession
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? Context { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Service for sending chatbot responses through the hub
/// </summary>
public interface IChatbotHubService
{
    Task SendResponseAsync(string userId, string sessionId, string response, bool isComplete = true);
    Task SendStreamingResponseAsync(string userId, string sessionId, string chunk, bool isComplete = false);
    Task SendTypingIndicatorAsync(string userId, string sessionId, bool isTyping);
    Task SendErrorAsync(string userId, string sessionId, string errorMessage);
    Task SendSuggestedActionsAsync(string userId, string sessionId, IEnumerable<string> actions);
}

public class ChatbotHubService : IChatbotHubService
{
    private readonly IHubContext<ChatbotHub> _hubContext;

    public ChatbotHubService(IHubContext<ChatbotHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendResponseAsync(string userId, string sessionId, string response, bool isComplete = true)
    {
        await _hubContext.Clients.Group($"chatbot_{userId}").SendAsync("ReceiveResponse", new
        {
            SessionId = sessionId,
            Response = response,
            IsComplete = isComplete,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task SendStreamingResponseAsync(string userId, string sessionId, string chunk, bool isComplete = false)
    {
        await _hubContext.Clients.Group($"chatbot_{userId}").SendAsync("ReceiveStreamingResponse", new
        {
            SessionId = sessionId,
            Chunk = chunk,
            IsComplete = isComplete,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task SendTypingIndicatorAsync(string userId, string sessionId, bool isTyping)
    {
        await _hubContext.Clients.Group($"chatbot_{userId}").SendAsync("BotTyping", new
        {
            SessionId = sessionId,
            IsTyping = isTyping
        });
    }

    public async Task SendErrorAsync(string userId, string sessionId, string errorMessage)
    {
        await _hubContext.Clients.Group($"chatbot_{userId}").SendAsync("ReceiveError", new
        {
            SessionId = sessionId,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task SendSuggestedActionsAsync(string userId, string sessionId, IEnumerable<string> actions)
    {
        await _hubContext.Clients.Group($"chatbot_{userId}").SendAsync("ReceiveSuggestedActions", new
        {
            SessionId = sessionId,
            Actions = actions,
            Timestamp = DateTime.UtcNow
        });
    }
}
