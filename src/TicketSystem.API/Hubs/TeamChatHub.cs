using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace TicketSystem.API.Hubs;

[Authorize]
public class TeamChatHub : Hub
{
    private static readonly Dictionary<string, UserPresence> _userPresence = new();
    private static readonly object _lock = new();

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

        if (!string.IsNullOrEmpty(userId))
        {
            lock (_lock)
            {
                _userPresence[userId] = new UserPresence
                {
                    UserId = userId,
                    UserName = userName,
                    ConnectionId = Context.ConnectionId,
                    Status = PresenceStatus.Online,
                    LastSeen = DateTime.UtcNow
                };
            }

            // Notify others that user is online
            await Clients.Others.SendAsync("UserOnline", new { UserId = userId, UserName = userName });
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            lock (_lock)
            {
                if (_userPresence.ContainsKey(userId))
                {
                    _userPresence[userId].Status = PresenceStatus.Offline;
                    _userPresence[userId].LastSeen = DateTime.UtcNow;
                }
            }

            await Clients.Others.SendAsync("UserOffline", new { UserId = userId, LastSeen = DateTime.UtcNow });
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a team chat room
    /// </summary>
    public async Task JoinTeam(int teamId)
    {
        var groupName = $"team_{teamId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        await Clients.Group(groupName).SendAsync("UserJoinedTeam", new
        {
            UserId = userId,
            UserName = userName,
            TeamId = teamId,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Leave a team chat room
    /// </summary>
    public async Task LeaveTeam(int teamId)
    {
        var groupName = $"team_{teamId}";

        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        await Clients.Group(groupName).SendAsync("UserLeftTeam", new
        {
            UserId = userId,
            UserName = userName,
            TeamId = teamId,
            Timestamp = DateTime.UtcNow
        });

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Join a group chat room
    /// </summary>
    public async Task JoinGroupChat(int groupChatId)
    {
        var groupName = $"groupchat_{groupChatId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        await Clients.Group(groupName).SendAsync("UserJoinedGroupChat", new
        {
            UserId = userId,
            UserName = userName,
            GroupChatId = groupChatId,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Leave a group chat room
    /// </summary>
    public async Task LeaveGroupChat(int groupChatId)
    {
        var groupName = $"groupchat_{groupChatId}";

        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        await Clients.Group(groupName).SendAsync("UserLeftGroupChat", new
        {
            UserId = userId,
            UserName = userName,
            GroupChatId = groupChatId,
            Timestamp = DateTime.UtcNow
        });

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Send a message to a team
    /// </summary>
    public async Task SendTeamMessage(int teamId, string content, string? messageType = "text")
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        var message = new
        {
            TeamId = teamId,
            SenderId = userId,
            SenderName = userName,
            Content = content,
            MessageType = messageType,
            Timestamp = DateTime.UtcNow
        };

        await Clients.Group($"team_{teamId}").SendAsync("ReceiveMessage", message);
    }

    /// <summary>
    /// Send a message to a group chat
    /// </summary>
    public async Task SendGroupMessage(int groupChatId, string content, string? messageType = "text")
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        var message = new
        {
            GroupChatId = groupChatId,
            SenderId = userId,
            SenderName = userName,
            Content = content,
            MessageType = messageType,
            Timestamp = DateTime.UtcNow
        };

        await Clients.Group($"groupchat_{groupChatId}").SendAsync("ReceiveMessage", message);
    }

    /// <summary>
    /// Send a direct message to a user
    /// </summary>
    public async Task SendDirectMessage(string targetUserId, string content)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        var message = new
        {
            SenderId = userId,
            SenderName = userName,
            TargetUserId = targetUserId,
            Content = content,
            Timestamp = DateTime.UtcNow
        };

        // Send to target user
        await Clients.Group($"user_{targetUserId}").SendAsync("ReceiveDirectMessage", message);
        // Send confirmation to sender
        await Clients.Caller.SendAsync("DirectMessageSent", message);
    }

    /// <summary>
    /// Notify typing status in a team
    /// </summary>
    public async Task TypingInTeam(int teamId, bool isTyping)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        await Clients.OthersInGroup($"team_{teamId}").SendAsync("UserTyping", new
        {
            UserId = userId,
            UserName = userName,
            TeamId = teamId,
            IsTyping = isTyping
        });
    }

    /// <summary>
    /// Notify typing status in a group chat
    /// </summary>
    public async Task TypingInGroupChat(int groupChatId, bool isTyping)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        await Clients.OthersInGroup($"groupchat_{groupChatId}").SendAsync("UserTyping", new
        {
            UserId = userId,
            UserName = userName,
            GroupChatId = groupChatId,
            IsTyping = isTyping
        });
    }

    /// <summary>
    /// Update user presence status
    /// </summary>
    public async Task UpdatePresence(string status)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId) && Enum.TryParse<PresenceStatus>(status, true, out var presenceStatus))
        {
            lock (_lock)
            {
                if (_userPresence.ContainsKey(userId))
                {
                    _userPresence[userId].Status = presenceStatus;
                    _userPresence[userId].LastSeen = DateTime.UtcNow;
                }
            }

            await Clients.Others.SendAsync("UserPresenceChanged", new
            {
                UserId = userId,
                Status = status,
                LastSeen = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get online users
    /// </summary>
    public Task<IEnumerable<UserPresence>> GetOnlineUsers()
    {
        lock (_lock)
        {
            var onlineUsers = _userPresence.Values
                .Where(u => u.Status == PresenceStatus.Online || u.Status == PresenceStatus.Away)
                .ToList();
            return Task.FromResult<IEnumerable<UserPresence>>(onlineUsers);
        }
    }

    /// <summary>
    /// Mark messages as read
    /// </summary>
    public async Task MarkMessagesAsRead(int? teamId, int? groupChatId, int lastReadMessageId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (teamId.HasValue)
        {
            await Clients.OthersInGroup($"team_{teamId}").SendAsync("MessagesRead", new
            {
                UserId = userId,
                TeamId = teamId,
                LastReadMessageId = lastReadMessageId
            });
        }
        else if (groupChatId.HasValue)
        {
            await Clients.OthersInGroup($"groupchat_{groupChatId}").SendAsync("MessagesRead", new
            {
                UserId = userId,
                GroupChatId = groupChatId,
                LastReadMessageId = lastReadMessageId
            });
        }
    }

    /// <summary>
    /// React to a message
    /// </summary>
    public async Task AddReaction(int messageId, string emoji, int? teamId, int? groupChatId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        var reaction = new
        {
            MessageId = messageId,
            UserId = userId,
            UserName = userName,
            Emoji = emoji,
            Timestamp = DateTime.UtcNow
        };

        if (teamId.HasValue)
        {
            await Clients.Group($"team_{teamId}").SendAsync("ReactionAdded", reaction);
        }
        else if (groupChatId.HasValue)
        {
            await Clients.Group($"groupchat_{groupChatId}").SendAsync("ReactionAdded", reaction);
        }
    }
}

public class UserPresence
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public PresenceStatus Status { get; set; }
    public DateTime LastSeen { get; set; }
}

public enum PresenceStatus
{
    Online,
    Away,
    Busy,
    DoNotDisturb,
    Offline
}

/// <summary>
/// Service for sending team chat messages through the hub
/// </summary>
public interface ITeamChatHubService
{
    Task SendTeamMessageAsync(int teamId, object message);
    Task SendGroupMessageAsync(int groupChatId, object message);
    Task SendDirectMessageAsync(string userId, object message);
    Task NotifyMessageEditedAsync(int? teamId, int? groupChatId, int messageId, string newContent);
    Task NotifyMessageDeletedAsync(int? teamId, int? groupChatId, int messageId);
}

public class TeamChatHubService : ITeamChatHubService
{
    private readonly IHubContext<TeamChatHub> _hubContext;

    public TeamChatHubService(IHubContext<TeamChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendTeamMessageAsync(int teamId, object message)
    {
        await _hubContext.Clients.Group($"team_{teamId}").SendAsync("ReceiveMessage", message);
    }

    public async Task SendGroupMessageAsync(int groupChatId, object message)
    {
        await _hubContext.Clients.Group($"groupchat_{groupChatId}").SendAsync("ReceiveMessage", message);
    }

    public async Task SendDirectMessageAsync(string userId, object message)
    {
        await _hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveDirectMessage", message);
    }

    public async Task NotifyMessageEditedAsync(int? teamId, int? groupChatId, int messageId, string newContent)
    {
        var update = new
        {
            MessageId = messageId,
            NewContent = newContent,
            EditedAt = DateTime.UtcNow
        };

        if (teamId.HasValue)
        {
            await _hubContext.Clients.Group($"team_{teamId}").SendAsync("MessageEdited", update);
        }
        else if (groupChatId.HasValue)
        {
            await _hubContext.Clients.Group($"groupchat_{groupChatId}").SendAsync("MessageEdited", update);
        }
    }

    public async Task NotifyMessageDeletedAsync(int? teamId, int? groupChatId, int messageId)
    {
        var update = new
        {
            MessageId = messageId,
            DeletedAt = DateTime.UtcNow
        };

        if (teamId.HasValue)
        {
            await _hubContext.Clients.Group($"team_{teamId}").SendAsync("MessageDeleted", update);
        }
        else if (groupChatId.HasValue)
        {
            await _hubContext.Clients.Group($"groupchat_{groupChatId}").SendAsync("MessageDeleted", update);
        }
    }
}
