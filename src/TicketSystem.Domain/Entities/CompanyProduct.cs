using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class CompanyProduct : BaseEntity
{
    public int CompanyId { get; set; }
    public int ProductId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Company Company { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ICollection<CompanyProductPlan> Plans { get; set; } = new List<CompanyProductPlan>();
}
