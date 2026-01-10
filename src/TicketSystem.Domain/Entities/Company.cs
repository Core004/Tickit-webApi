using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class Company : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Logo { get; set; }
    public string? Website { get; set; }
    public string? MobileNo { get; set; }
    public string? PhoneNo { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? Area { get; set; }
    public string? PinCode { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<DepartmentCompany> DepartmentCompanies { get; set; } = new List<DepartmentCompany>();
    public ICollection<CompanyProduct> CompanyProducts { get; set; } = new List<CompanyProduct>();
    public ICollection<CompanyPriority> CompanyPriorities { get; set; } = new List<CompanyPriority>();
}
