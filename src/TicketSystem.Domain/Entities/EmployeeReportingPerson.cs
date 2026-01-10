using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class EmployeeReportingPerson : BaseEntity
{
    public int EmployeeId { get; set; }
    public int ReportingPersonId { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime AssignedAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Employee Employee { get; set; } = null!;
    public Employee ReportingPerson { get; set; } = null!;
}
