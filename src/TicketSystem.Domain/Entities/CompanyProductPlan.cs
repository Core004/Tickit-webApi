using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class CompanyProductPlan : BaseEntity
{
    public int CompanyProductId { get; set; }
    public int ProductPlanId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public CompanyProduct CompanyProduct { get; set; } = null!;
    public ProductPlan ProductPlan { get; set; } = null!;
}
