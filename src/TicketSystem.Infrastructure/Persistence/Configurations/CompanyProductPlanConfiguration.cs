using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Infrastructure.Persistence.Configurations;

public class CompanyProductPlanConfiguration : IEntityTypeConfiguration<CompanyProductPlan>
{
    public void Configure(EntityTypeBuilder<CompanyProductPlan> builder)
    {
        builder.HasKey(cpp => cpp.Id);

        builder.HasOne(cpp => cpp.CompanyProduct)
            .WithMany(cp => cp.Plans)
            .HasForeignKey(cpp => cpp.CompanyProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cpp => cpp.ProductPlan)
            .WithMany(pp => pp.CompanyProductPlans)
            .HasForeignKey(cpp => cpp.ProductPlanId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
