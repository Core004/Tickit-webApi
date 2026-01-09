using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class RevokedToken : BaseEntity
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime RevokedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? Reason { get; set; }
}
