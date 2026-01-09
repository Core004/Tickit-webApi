using TicketSystem.Domain.Common;
using TicketSystem.Domain.Enums;
using TicketSystem.Domain.Events;

namespace TicketSystem.Domain.Entities;

public class Ticket : AuditableEntity
{
    public string TicketNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    // Foreign keys
    public int? CategoryId { get; set; }
    public int? StatusId { get; set; }
    public int? PriorityId { get; set; }
    public int? CompanyId { get; set; }
    public int? DepartmentId { get; set; }
    public int? TeamId { get; set; }
    public string? AssignedToId { get; set; }
    public new string CreatedById { get; set; } = string.Empty;
    public int? MergedIntoTicketId { get; set; }

    // SLA fields
    public DateTime? DueDate { get; set; }
    public DateTime? FirstResponseAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool IsSLABreached { get; set; }

    // Additional fields
    public string? Source { get; set; }
    public string? Tags { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public TicketCategory? Category { get; set; }
    public TicketStatusEntity? Status { get; set; }
    public Priority? PriorityEntity { get; set; }
    public Company? Company { get; set; }
    public Department? Department { get; set; }
    public Team? Team { get; set; }
    public ApplicationUser? AssignedTo { get; set; }
    public ApplicationUser? CreatedBy { get; set; }
    public Ticket? MergedIntoTicket { get; set; }

    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
    public ICollection<TicketHistory> History { get; set; } = new List<TicketHistory>();
    public ICollection<TicketCustomField> CustomFields { get; set; } = new List<TicketCustomField>();
    public ICollection<TicketLink> SourceLinks { get; set; } = new List<TicketLink>();
    public ICollection<TicketLink> TargetLinks { get; set; } = new List<TicketLink>();
    public ICollection<Ticket> MergedTickets { get; set; } = new List<Ticket>();

    // Domain methods
    public void Assign(string userId)
    {
        AssignedToId = userId;
        AddDomainEvent(new TicketAssignedEvent(Id, userId));
    }

    public void ChangeStatus(int statusId)
    {
        var previousStatusId = StatusId;
        StatusId = statusId;
        AddDomainEvent(new TicketStatusChangedEvent(Id, previousStatusId, statusId));
    }

    public static Ticket Create(string title, string description, string createdById, TicketPriority priority = TicketPriority.Medium)
    {
        var ticket = new Ticket
        {
            Title = title,
            Description = description,
            CreatedById = createdById,
            Priority = priority,
            TicketNumber = GenerateTicketNumber()
        };

        ticket.AddDomainEvent(new TicketCreatedEvent(ticket.Id, createdById));
        return ticket;
    }

    private static string GenerateTicketNumber()
    {
        return $"TKT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }
}
