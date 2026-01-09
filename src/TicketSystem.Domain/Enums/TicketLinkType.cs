namespace TicketSystem.Domain.Enums;

public enum TicketLinkType
{
    RelatedTo = 1,
    BlockedBy = 2,
    Blocks = 3,
    DuplicateOf = 4,
    ClonedFrom = 5,
    ParentOf = 6,
    ChildOf = 7
}
