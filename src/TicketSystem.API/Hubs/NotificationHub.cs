using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace TicketSystem.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private static readonly Dictionary<string, HashSet<string>> _userConnections = new();
    private static readonly object _lock = new();

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            lock (_lock)
            {
                if (!_userConnections.ContainsKey(userId))
                {
                    _userConnections[userId] = new HashSet<string>();
                }
                _userConnections[userId].Add(Context.ConnectionId);
            }

            // Add user to their personal group for targeted notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
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
                if (_userConnections.ContainsKey(userId))
                {
                    _userConnections[userId].Remove(Context.ConnectionId);
                    if (_userConnections[userId].Count == 0)
                    {
                        _userConnections.Remove(userId);
                    }
                }
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a specific notification group (e.g., for team or department notifications)
    /// </summary>
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Leave a notification group
    /// </summary>
    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    public async Task MarkAsRead(int notificationId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            // This would typically call a service to mark the notification as read
            // For now, we'll just acknowledge the action
            await Clients.Caller.SendAsync("NotificationMarkedAsRead", notificationId);
        }
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    public async Task MarkAllAsRead()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("AllNotificationsMarkedAsRead");
        }
    }

    /// <summary>
    /// Check if a specific user is online
    /// </summary>
    public static bool IsUserOnline(string userId)
    {
        lock (_lock)
        {
            return _userConnections.ContainsKey(userId) && _userConnections[userId].Count > 0;
        }
    }

    /// <summary>
    /// Get all connection IDs for a user
    /// </summary>
    public static IEnumerable<string> GetUserConnections(string userId)
    {
        lock (_lock)
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                return connections.ToList();
            }
            return Enumerable.Empty<string>();
        }
    }
}

/// <summary>
/// Service for sending notifications through the hub
/// </summary>
public interface INotificationHubService
{
    Task SendNotificationAsync(string userId, object notification);
    Task SendNotificationToGroupAsync(string groupName, object notification);
    Task SendNotificationToAllAsync(object notification);
    Task SendTicketUpdateAsync(string userId, int ticketId, string updateType, object data);
}

public class NotificationHubService : INotificationHubService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationHubService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendNotificationAsync(string userId, object notification)
    {
        await _hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", notification);
    }

    public async Task SendNotificationToGroupAsync(string groupName, object notification)
    {
        await _hubContext.Clients.Group(groupName).SendAsync("ReceiveNotification", notification);
    }

    public async Task SendNotificationToAllAsync(object notification)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
    }

    public async Task SendTicketUpdateAsync(string userId, int ticketId, string updateType, object data)
    {
        await _hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveTicketUpdate", new
        {
            TicketId = ticketId,
            UpdateType = updateType,
            Data = data,
            Timestamp = DateTime.UtcNow
        });
    }
}
