using Microsoft.EntityFrameworkCore;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Infrastructure.Persistence.Seeding;

public static class StatusSeeder
{
    public static async Task SeedStatusesAsync(ApplicationDbContext context)
    {
        if (await context.TicketStatuses.AnyAsync())
            return;

        var statuses = new List<TicketStatusEntity>
        {
            new() { Name = "Open", Color = "#3B82F6", Icon = "circle", DisplayOrder = 1, IsDefault = true, CreatedAt = DateTime.UtcNow },
            new() { Name = "In Progress", Color = "#F59E0B", Icon = "clock", DisplayOrder = 2, CreatedAt = DateTime.UtcNow },
            new() { Name = "Pending", Color = "#8B5CF6", Icon = "pause", DisplayOrder = 3, CreatedAt = DateTime.UtcNow },
            new() { Name = "Resolved", Color = "#10B981", Icon = "check", DisplayOrder = 4, IsResolved = true, CreatedAt = DateTime.UtcNow },
            new() { Name = "Closed", Color = "#6B7280", Icon = "x", DisplayOrder = 5, IsClosed = true, CreatedAt = DateTime.UtcNow },
            new() { Name = "Cancelled", Color = "#EF4444", Icon = "ban", DisplayOrder = 6, IsClosed = true, CreatedAt = DateTime.UtcNow }
        };

        context.TicketStatuses.AddRange(statuses);
        await context.SaveChangesAsync();
    }

    public static async Task SeedPrioritiesAsync(ApplicationDbContext context)
    {
        if (await context.Priorities.AnyAsync())
            return;

        var priorities = new List<Priority>
        {
            new() { Name = "Low", Color = "#6B7280", Level = 1, DisplayOrder = 1, ResponseTimeMinutes = 1440, ResolutionTimeMinutes = 10080, CreatedAt = DateTime.UtcNow },
            new() { Name = "Medium", Color = "#3B82F6", Level = 2, DisplayOrder = 2, IsDefault = true, ResponseTimeMinutes = 480, ResolutionTimeMinutes = 2880, CreatedAt = DateTime.UtcNow },
            new() { Name = "High", Color = "#F59E0B", Level = 3, DisplayOrder = 3, ResponseTimeMinutes = 240, ResolutionTimeMinutes = 1440, CreatedAt = DateTime.UtcNow },
            new() { Name = "Critical", Color = "#EF4444", Level = 4, DisplayOrder = 4, ResponseTimeMinutes = 60, ResolutionTimeMinutes = 480, CreatedAt = DateTime.UtcNow },
            new() { Name = "Urgent", Color = "#DC2626", Level = 5, DisplayOrder = 5, ResponseTimeMinutes = 30, ResolutionTimeMinutes = 240, CreatedAt = DateTime.UtcNow }
        };

        context.Priorities.AddRange(priorities);
        await context.SaveChangesAsync();
    }

    public static async Task SeedCategoriesAsync(ApplicationDbContext context)
    {
        if (await context.TicketCategories.AnyAsync())
            return;

        var categories = new List<TicketCategory>
        {
            new() { Name = "General", Description = "General inquiries", Color = "#6B7280", DisplayOrder = 1, CreatedAt = DateTime.UtcNow },
            new() { Name = "Technical Support", Description = "Technical issues and support requests", Color = "#3B82F6", DisplayOrder = 2, CreatedAt = DateTime.UtcNow },
            new() { Name = "Billing", Description = "Billing and payment related", Color = "#10B981", DisplayOrder = 3, CreatedAt = DateTime.UtcNow },
            new() { Name = "Feature Request", Description = "New feature requests", Color = "#8B5CF6", DisplayOrder = 4, CreatedAt = DateTime.UtcNow },
            new() { Name = "Bug Report", Description = "Bug reports and issues", Color = "#EF4444", DisplayOrder = 5, CreatedAt = DateTime.UtcNow }
        };

        context.TicketCategories.AddRange(categories);
        await context.SaveChangesAsync();
    }
}
