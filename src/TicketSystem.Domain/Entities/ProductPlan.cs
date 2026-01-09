using TicketSystem.Domain.Common;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Domain.Entities;

public class ProductPlan : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public BillingCycle BillingCycle { get; set; }
    public bool IsActive { get; set; } = true;

    // Foreign keys
    public int ProductId { get; set; }

    // Navigation properties
    public Product Product { get; set; } = null!;
    public ICollection<CompanyProductPlan> CompanyProductPlans { get; set; } = new List<CompanyProductPlan>();
}
