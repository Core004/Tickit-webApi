using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class BusinessHours : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string TimeZone { get; set; } = "UTC";
    public bool IsWorkingDay { get; set; } = true;
    public bool IsActive { get; set; } = true;

    // Optional: Company-specific business hours
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }
}
