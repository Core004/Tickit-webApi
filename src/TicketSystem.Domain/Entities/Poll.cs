using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class Poll : AuditableEntity
{
    public string Question { get; set; } = string.Empty;
    public int? TeamId { get; set; }
    public int? GroupChatId { get; set; }
    public int? MessageId { get; set; } // The message containing this poll
    public bool IsAnonymous { get; set; }
    public bool AllowMultipleVotes { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsClosed { get; set; }
    public DateTime? ClosedAt { get; set; }

    // Navigation properties
    public Team? Team { get; set; }
    public GroupChat? GroupChat { get; set; }
    public TeamMessage? Message { get; set; }
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public ICollection<PollOption> Options { get; set; } = new List<PollOption>();
}

public class PollOption : BaseEntity
{
    public int PollId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int VoteCount { get; set; }

    // Navigation properties
    public Poll Poll { get; set; } = null!;
    public ICollection<PollVote> Votes { get; set; } = new List<PollVote>();
}

public class PollVote : BaseEntity
{
    public int PollOptionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime VotedAt { get; set; }

    // Navigation properties
    public PollOption PollOption { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
