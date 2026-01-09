using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class Product : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Logo { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<ProductPlan> Plans { get; set; } = new List<ProductPlan>();
    public ICollection<ProductVersion> Versions { get; set; } = new List<ProductVersion>();
    public ICollection<CompanyProduct> CompanyProducts { get; set; } = new List<CompanyProduct>();
}
