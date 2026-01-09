using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Events;

public class TicketStatusChangedEvent : IDomainEvent
{
    public int TicketId { get; }
    public int? PreviousStatusId { get; }
    public int? NewStatusId { get; }
    public DateTime OccurredOn { get; }

    public TicketStatusChangedEvent(int ticketId, int? previousStatusId, int? newStatusId)
    {
        TicketId = ticketId;
        PreviousStatusId = previousStatusId;
        NewStatusId = newStatusId;
        OccurredOn = DateTime.UtcNow;
    }
}
