using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class Employee : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public DateTime JoinDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Optional link to User
    public string? UserId { get; set; }

    // Organization hierarchy
    public int? CompanyId { get; set; }
    public int? DepartmentId { get; set; }
    public int? TeamId { get; set; }

    // Navigation properties
    public ApplicationUser? User { get; set; }
    public Company? Company { get; set; }
    public Department? Department { get; set; }
    public Team? Team { get; set; }
    public ICollection<EmployeeReportingPerson> ReportingPersons { get; set; } = new List<EmployeeReportingPerson>();
    public ICollection<EmployeeReportingPerson> Subordinates { get; set; } = new List<EmployeeReportingPerson>();
}
