using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

namespace TicketSystem.API.Controllers;

[Authorize]
[Route("api/team-chat")]
[ApiController]
public class TeamChatController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<TeamChatController> _logger;

    public TeamChatController(IApplicationDbContext context, ILogger<TeamChatController> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Group Chats

    [HttpGet("groups")]
    public async Task<ActionResult<List<GroupChatDto>>> GetMyGroupChats()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var groups = await _context.GroupChatMembers
            .Where(m => m.UserId == userId && m.LeftAt == null)
            .Include(m => m.GroupChat)
                .ThenInclude(g => g.Members)
            .Select(m => new GroupChatDto
            {
                Id = m.GroupChat.Id,
                Name = m.GroupChat.Name,
                Description = m.GroupChat.Description,
                AvatarUrl = m.GroupChat.AvatarUrl,
                IsPrivate = m.GroupChat.IsPrivate,
                MemberCount = m.GroupChat.Members.Count(x => x.LeftAt == null),
                IsAdmin = m.IsAdmin,
                IsMuted = m.IsMuted,
                LastReadAt = m.LastReadAt,
                CreatedAt = m.GroupChat.CreatedAt
            })
            .ToListAsync();

        return Ok(groups);
    }

    [HttpGet("groups/{id}")]
    public async Task<ActionResult<GroupChatDetailDto>> GetGroupChat(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var membership = await _context.GroupChatMembers
            .Include(m => m.GroupChat)
                .ThenInclude(g => g.CreatedBy)
            .Include(m => m.GroupChat)
                .ThenInclude(g => g.Members)
                    .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(m => m.GroupChatId == id && m.UserId == userId && m.LeftAt == null);

        if (membership is null)
            return NotFound(new { Message = "Group not found or you are not a member" });

        var group = membership.GroupChat;

        return Ok(new GroupChatDetailDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            AvatarUrl = group.AvatarUrl,
            IsPrivate = group.IsPrivate,
            CreatedById = group.CreatedById,
            CreatedByName = group.CreatedBy.FullName,
            CreatedAt = group.CreatedAt,
            Members = group.Members
                .Where(m => m.LeftAt == null)
                .Select(m => new GroupChatMemberDto
                {
                    UserId = m.UserId,
                    UserName = m.User.FullName,
                    Email = m.User.Email!,
                    IsAdmin = m.IsAdmin,
                    JoinedAt = m.JoinedAt
                }).ToList()
        });
    }

    [HttpPost("groups")]
    public async Task<ActionResult<int>> CreateGroupChat([FromBody] CreateGroupChatRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;

        var group = new GroupChat
        {
            Name = request.Name,
            Description = request.Description,
            IsPrivate = request.IsPrivate,
            CreatedById = userId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.GroupChats.Add(group);
        await _context.SaveChangesAsync();

        // Add creator as admin
        var creatorMember = new GroupChatMember
        {
            GroupChatId = group.Id,
            UserId = userId,
            IsAdmin = true,
            JoinedAt = DateTime.UtcNow
        };
        _context.GroupChatMembers.Add(creatorMember);

        // Add initial members
        if (request.MemberIds?.Any() == true)
        {
            foreach (var memberId in request.MemberIds.Where(m => m != userId))
            {
                _context.GroupChatMembers.Add(new GroupChatMember
                {
                    GroupChatId = group.Id,
                    UserId = memberId,
                    IsAdmin = false,
                    JoinedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Group chat {Name} created by {UserId}", group.Name, userId);

        return CreatedAtAction(nameof(GetGroupChat), new { id = group.Id }, group.Id);
    }

    [HttpPut("groups/{id}")]
    public async Task<IActionResult> UpdateGroupChat(int id, [FromBody] UpdateGroupChatRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var membership = await _context.GroupChatMembers
            .Include(m => m.GroupChat)
            .FirstOrDefaultAsync(m => m.GroupChatId == id && m.UserId == userId && m.IsAdmin && m.LeftAt == null);

        if (membership is null)
            return NotFound(new { Message = "Group not found or you are not an admin" });

        membership.GroupChat.Name = request.Name;
        membership.GroupChat.Description = request.Description;
        membership.GroupChat.IsPrivate = request.IsPrivate;
        membership.GroupChat.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("groups/{id}/members")]
    public async Task<IActionResult> AddGroupMember(int id, [FromBody] AddGroupMemberRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var membership = await _context.GroupChatMembers
            .FirstOrDefaultAsync(m => m.GroupChatId == id && m.UserId == userId && m.IsAdmin && m.LeftAt == null);

        if (membership is null)
            return NotFound(new { Message = "Group not found or you are not an admin" });

        // Check if user is already a member
        var existingMember = await _context.GroupChatMembers
            .FirstOrDefaultAsync(m => m.GroupChatId == id && m.UserId == request.UserId);

        if (existingMember != null)
        {
            if (existingMember.LeftAt != null)
            {
                existingMember.LeftAt = null;
                existingMember.JoinedAt = DateTime.UtcNow;
            }
            else
            {
                return BadRequest(new { Message = "User is already a member" });
            }
        }
        else
        {
            _context.GroupChatMembers.Add(new GroupChatMember
            {
                GroupChatId = id,
                UserId = request.UserId,
                IsAdmin = request.IsAdmin,
                JoinedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("groups/{id}/members/{memberId}")]
    public async Task<IActionResult> RemoveGroupMember(int id, string memberId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var adminMembership = await _context.GroupChatMembers
            .FirstOrDefaultAsync(m => m.GroupChatId == id && m.UserId == userId && m.IsAdmin && m.LeftAt == null);

        if (adminMembership is null && memberId != userId)
            return NotFound(new { Message = "Group not found or you are not an admin" });

        var targetMembership = await _context.GroupChatMembers
            .FirstOrDefaultAsync(m => m.GroupChatId == id && m.UserId == memberId && m.LeftAt == null);

        if (targetMembership is null)
            return NotFound(new { Message = "Member not found" });

        targetMembership.LeftAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("groups/{id}/leave")]
    public async Task<IActionResult> LeaveGroup(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var membership = await _context.GroupChatMembers
            .FirstOrDefaultAsync(m => m.GroupChatId == id && m.UserId == userId && m.LeftAt == null);

        if (membership is null)
            return NotFound();

        membership.LeftAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("groups/{id}")]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var group = await _context.GroupChats
            .FirstOrDefaultAsync(g => g.Id == id && g.CreatedById == userId);

        if (group is null)
            return NotFound(new { Message = "Group not found or you are not the creator" });

        group.IsActive = false;
        group.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    #endregion

    #region Team Messages

    [HttpGet("teams/{teamId}/messages")]
    public async Task<ActionResult<List<TeamMessageDto>>> GetTeamMessages(
        int teamId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] DateTime? before = null)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        // Check if user is team member
        var isMember = await _context.TeamMembers
            .AnyAsync(m => m.TeamId == teamId && m.UserId == userId && m.IsActive);

        if (!isMember)
            return Forbid();

        var query = _context.TeamMessages
            .Where(m => m.TeamId == teamId && !m.IsDeleted)
            .Include(m => m.Sender)
            .Include(m => m.Reactions)
            .Include(m => m.Attachments)
            .AsQueryable();

        if (before.HasValue)
            query = query.Where(m => m.CreatedAt < before.Value);

        var messages = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new TeamMessageDto
            {
                Id = m.Id,
                Content = m.Content,
                MessageType = m.MessageType,
                SenderId = m.SenderId,
                SenderName = m.Sender.FullName,
                IsEdited = m.IsEdited,
                ReplyToMessageId = m.ReplyToMessageId,
                CreatedAt = m.CreatedAt,
                Reactions = m.Reactions.GroupBy(r => r.Emoji)
                    .Select(g => new ReactionGroupDto { Emoji = g.Key, Count = g.Count() }).ToList(),
                AttachmentCount = m.Attachments.Count
            })
            .ToListAsync();

        return Ok(messages.AsEnumerable().Reverse().ToList());
    }

    [HttpGet("groups/{groupId}/messages")]
    public async Task<ActionResult<List<TeamMessageDto>>> GetGroupMessages(
        int groupId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] DateTime? before = null)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        // Check if user is group member
        var isMember = await _context.GroupChatMembers
            .AnyAsync(m => m.GroupChatId == groupId && m.UserId == userId && m.LeftAt == null);

        if (!isMember)
            return Forbid();

        var query = _context.TeamMessages
            .Where(m => m.GroupChatId == groupId && !m.IsDeleted)
            .Include(m => m.Sender)
            .Include(m => m.Reactions)
            .Include(m => m.Attachments)
            .AsQueryable();

        if (before.HasValue)
            query = query.Where(m => m.CreatedAt < before.Value);

        var messages = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new TeamMessageDto
            {
                Id = m.Id,
                Content = m.Content,
                MessageType = m.MessageType,
                SenderId = m.SenderId,
                SenderName = m.Sender.FullName,
                IsEdited = m.IsEdited,
                ReplyToMessageId = m.ReplyToMessageId,
                CreatedAt = m.CreatedAt,
                Reactions = m.Reactions.GroupBy(r => r.Emoji)
                    .Select(g => new ReactionGroupDto { Emoji = g.Key, Count = g.Count() }).ToList(),
                AttachmentCount = m.Attachments.Count
            })
            .ToListAsync();

        return Ok(messages.AsEnumerable().Reverse().ToList());
    }

    [HttpPost("teams/{teamId}/messages")]
    public async Task<ActionResult<TeamMessageDto>> SendTeamMessage(int teamId, [FromBody] SendMessageRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;

        // Check if user is team member
        var isMember = await _context.TeamMembers
            .AnyAsync(m => m.TeamId == teamId && m.UserId == userId && m.IsActive);

        if (!isMember)
            return Forbid();

        var message = new TeamMessage
        {
            TeamId = teamId,
            SenderId = userId,
            Content = request.Content,
            MessageType = request.MessageType,
            ReplyToMessageId = request.ReplyToMessageId,
            CreatedAt = DateTime.UtcNow
        };

        _context.TeamMessages.Add(message);
        await _context.SaveChangesAsync();

        var sender = await _context.TeamMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.User.FullName)
            .FirstOrDefaultAsync() ?? "Unknown";

        return Ok(new TeamMessageDto
        {
            Id = message.Id,
            Content = message.Content,
            MessageType = message.MessageType,
            SenderId = message.SenderId,
            SenderName = sender,
            IsEdited = false,
            ReplyToMessageId = message.ReplyToMessageId,
            CreatedAt = message.CreatedAt,
            Reactions = new List<ReactionGroupDto>(),
            AttachmentCount = 0
        });
    }

    [HttpPost("groups/{groupId}/messages")]
    public async Task<ActionResult<TeamMessageDto>> SendGroupMessage(int groupId, [FromBody] SendMessageRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;

        // Check if user is group member
        var membership = await _context.GroupChatMembers
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.GroupChatId == groupId && m.UserId == userId && m.LeftAt == null);

        if (membership is null)
            return Forbid();

        var message = new TeamMessage
        {
            GroupChatId = groupId,
            SenderId = userId,
            Content = request.Content,
            MessageType = request.MessageType,
            ReplyToMessageId = request.ReplyToMessageId,
            CreatedAt = DateTime.UtcNow
        };

        _context.TeamMessages.Add(message);
        await _context.SaveChangesAsync();

        return Ok(new TeamMessageDto
        {
            Id = message.Id,
            Content = message.Content,
            MessageType = message.MessageType,
            SenderId = message.SenderId,
            SenderName = membership.User.FullName,
            IsEdited = false,
            ReplyToMessageId = message.ReplyToMessageId,
            CreatedAt = message.CreatedAt,
            Reactions = new List<ReactionGroupDto>(),
            AttachmentCount = 0
        });
    }

    [HttpPut("messages/{messageId}")]
    public async Task<IActionResult> EditMessage(int messageId, [FromBody] EditMessageRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var message = await _context.TeamMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == userId && !m.IsDeleted);

        if (message is null)
            return NotFound();

        // Save edit history
        _context.TeamMessageEditHistories.Add(new TeamMessageEditHistory
        {
            MessageId = messageId,
            PreviousContent = message.Content,
            EditedById = userId!,
            EditedAt = DateTime.UtcNow
        });

        message.Content = request.Content;
        message.IsEdited = true;
        message.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("messages/{messageId}")]
    public async Task<IActionResult> DeleteMessage(int messageId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var message = await _context.TeamMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == userId && !m.IsDeleted);

        if (message is null)
            return NotFound();

        message.IsDeleted = true;
        message.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    #endregion

    #region Reactions

    [HttpPost("messages/{messageId}/reactions")]
    public async Task<IActionResult> AddReaction(int messageId, [FromBody] AddReactionRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;

        var message = await _context.TeamMessages.FindAsync(messageId);
        if (message is null || message.IsDeleted)
            return NotFound();

        // Check if user already reacted with this emoji
        var existingReaction = await _context.TeamMessageReactions
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId && r.Emoji == request.Emoji);

        if (existingReaction != null)
            return BadRequest(new { Message = "You already reacted with this emoji" });

        _context.TeamMessageReactions.Add(new TeamMessageReaction
        {
            MessageId = messageId,
            UserId = userId,
            Emoji = request.Emoji,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("messages/{messageId}/reactions/{emoji}")]
    public async Task<IActionResult> RemoveReaction(int messageId, string emoji)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var reaction = await _context.TeamMessageReactions
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId && r.Emoji == emoji);

        if (reaction is null)
            return NotFound();

        _context.TeamMessageReactions.Remove(reaction);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    #endregion

    #region Read Status

    [HttpPost("groups/{groupId}/read")]
    public async Task<IActionResult> MarkGroupAsRead(int groupId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var membership = await _context.GroupChatMembers
            .FirstOrDefaultAsync(m => m.GroupChatId == groupId && m.UserId == userId && m.LeftAt == null);

        if (membership is null)
            return NotFound();

        membership.LastReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("groups/{groupId}/mute")]
    public async Task<IActionResult> MuteGroup(int groupId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var membership = await _context.GroupChatMembers
            .FirstOrDefaultAsync(m => m.GroupChatId == groupId && m.UserId == userId && m.LeftAt == null);

        if (membership is null)
            return NotFound();

        membership.IsMuted = true;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("groups/{groupId}/unmute")]
    public async Task<IActionResult> UnmuteGroup(int groupId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var membership = await _context.GroupChatMembers
            .FirstOrDefaultAsync(m => m.GroupChatId == groupId && m.UserId == userId && m.LeftAt == null);

        if (membership is null)
            return NotFound();

        membership.IsMuted = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    #endregion
}

// DTOs
public class GroupChatDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsPrivate { get; set; }
    public int MemberCount { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsMuted { get; set; }
    public DateTime? LastReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GroupChatDetailDto : GroupChatDto
{
    public string CreatedById { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public List<GroupChatMemberDto> Members { get; set; } = new();
}

public class GroupChatMemberDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class TeamMessageDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public MessageType MessageType { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public bool IsEdited { get; set; }
    public int? ReplyToMessageId { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ReactionGroupDto> Reactions { get; set; } = new();
    public int AttachmentCount { get; set; }
}

public class ReactionGroupDto
{
    public string Emoji { get; set; } = string.Empty;
    public int Count { get; set; }
}

public record CreateGroupChatRequest(
    string Name,
    string? Description = null,
    bool IsPrivate = false,
    List<string>? MemberIds = null);

public record UpdateGroupChatRequest(
    string Name,
    string? Description = null,
    bool IsPrivate = false);

public record AddGroupMemberRequest(string UserId, bool IsAdmin = false);

public record SendMessageRequest(
    string Content,
    MessageType MessageType = MessageType.Text,
    int? ReplyToMessageId = null);

public record EditMessageRequest(string Content);

public record AddReactionRequest(string Emoji);
