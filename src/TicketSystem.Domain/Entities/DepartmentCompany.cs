namespace TicketSystem.Domain.Entities;

public class DepartmentCompany
{
    public int DepartmentId { get; set; }
    public int CompanyId { get; set; }

    // Navigation properties
    public Department Department { get; set; } = null!;
    public Company Company { get; set; } = null!;
}
