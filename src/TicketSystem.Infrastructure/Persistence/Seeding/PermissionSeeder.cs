using Microsoft.EntityFrameworkCore;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Infrastructure.Persistence.Seeding;

public static class PermissionSeeder
{
    public static async Task SeedPermissionsAsync(ApplicationDbContext context)
    {
        if (await context.Permissions.AnyAsync())
            return;

        var permissions = new List<Permission>
        {
            // Tickets
            new() { Name = "Tickets.View", Category = "Tickets", Description = "View tickets" },
            new() { Name = "Tickets.ViewAll", Category = "Tickets", Description = "View all tickets" },
            new() { Name = "Tickets.Create", Category = "Tickets", Description = "Create tickets" },
            new() { Name = "Tickets.Update", Category = "Tickets", Description = "Update tickets" },
            new() { Name = "Tickets.Delete", Category = "Tickets", Description = "Delete tickets" },
            new() { Name = "Tickets.Assign", Category = "Tickets", Description = "Assign tickets" },
            new() { Name = "Tickets.ChangeStatus", Category = "Tickets", Description = "Change ticket status" },
            new() { Name = "Tickets.Merge", Category = "Tickets", Description = "Merge tickets" },
            new() { Name = "Tickets.Export", Category = "Tickets", Description = "Export tickets" },

            // Comments
            new() { Name = "Comments.View", Category = "Comments", Description = "View comments" },
            new() { Name = "Comments.Create", Category = "Comments", Description = "Create comments" },
            new() { Name = "Comments.CreateInternal", Category = "Comments", Description = "Create internal comments" },
            new() { Name = "Comments.Delete", Category = "Comments", Description = "Delete comments" },

            // Users
            new() { Name = "Users.View", Category = "Users", Description = "View users" },
            new() { Name = "Users.Create", Category = "Users", Description = "Create users" },
            new() { Name = "Users.Update", Category = "Users", Description = "Update users" },
            new() { Name = "Users.Delete", Category = "Users", Description = "Delete users" },
            new() { Name = "Users.ManageRoles", Category = "Users", Description = "Manage user roles" },
            new() { Name = "Users.ManagePermissions", Category = "Users", Description = "Manage user permissions" },

            // Companies
            new() { Name = "Companies.View", Category = "Companies", Description = "View companies" },
            new() { Name = "Companies.Create", Category = "Companies", Description = "Create companies" },
            new() { Name = "Companies.Update", Category = "Companies", Description = "Update companies" },
            new() { Name = "Companies.Delete", Category = "Companies", Description = "Delete companies" },

            // Departments
            new() { Name = "Departments.View", Category = "Departments", Description = "View departments" },
            new() { Name = "Departments.Create", Category = "Departments", Description = "Create departments" },
            new() { Name = "Departments.Update", Category = "Departments", Description = "Update departments" },
            new() { Name = "Departments.Delete", Category = "Departments", Description = "Delete departments" },
            new() { Name = "Departments.ManageMembers", Category = "Departments", Description = "Manage department members" },

            // Teams
            new() { Name = "Teams.View", Category = "Teams", Description = "View teams" },
            new() { Name = "Teams.Create", Category = "Teams", Description = "Create teams" },
            new() { Name = "Teams.Update", Category = "Teams", Description = "Update teams" },
            new() { Name = "Teams.Delete", Category = "Teams", Description = "Delete teams" },
            new() { Name = "Teams.ManageMembers", Category = "Teams", Description = "Manage team members" },

            // Categories
            new() { Name = "Categories.View", Category = "Categories", Description = "View categories" },
            new() { Name = "Categories.Create", Category = "Categories", Description = "Create categories" },
            new() { Name = "Categories.Update", Category = "Categories", Description = "Update categories" },
            new() { Name = "Categories.Delete", Category = "Categories", Description = "Delete categories" },

            // Statuses
            new() { Name = "Statuses.View", Category = "Statuses", Description = "View statuses" },
            new() { Name = "Statuses.Create", Category = "Statuses", Description = "Create statuses" },
            new() { Name = "Statuses.Update", Category = "Statuses", Description = "Update statuses" },
            new() { Name = "Statuses.Delete", Category = "Statuses", Description = "Delete statuses" },

            // Priorities
            new() { Name = "Priorities.View", Category = "Priorities", Description = "View priorities" },
            new() { Name = "Priorities.Create", Category = "Priorities", Description = "Create priorities" },
            new() { Name = "Priorities.Update", Category = "Priorities", Description = "Update priorities" },
            new() { Name = "Priorities.Delete", Category = "Priorities", Description = "Delete priorities" },

            // SLA
            new() { Name = "SLA.View", Category = "SLA", Description = "View SLA rules" },
            new() { Name = "SLA.Create", Category = "SLA", Description = "Create SLA rules" },
            new() { Name = "SLA.Update", Category = "SLA", Description = "Update SLA rules" },
            new() { Name = "SLA.Delete", Category = "SLA", Description = "Delete SLA rules" },

            // Knowledge Base
            new() { Name = "KnowledgeBase.View", Category = "KnowledgeBase", Description = "View knowledge base articles" },
            new() { Name = "KnowledgeBase.Create", Category = "KnowledgeBase", Description = "Create knowledge base articles" },
            new() { Name = "KnowledgeBase.Update", Category = "KnowledgeBase", Description = "Update knowledge base articles" },
            new() { Name = "KnowledgeBase.Delete", Category = "KnowledgeBase", Description = "Delete knowledge base articles" },
            new() { Name = "KnowledgeBase.Publish", Category = "KnowledgeBase", Description = "Publish knowledge base articles" },

            // Analytics
            new() { Name = "Analytics.View", Category = "Analytics", Description = "View analytics dashboard" },
            new() { Name = "Analytics.Export", Category = "Analytics", Description = "Export analytics reports" },

            // Admin
            new() { Name = "Admin.Settings", Category = "Admin", Description = "Access admin settings" },
            new() { Name = "Admin.AuditLogs", Category = "Admin", Description = "View audit logs" },
            new() { Name = "Admin.ManageRoles", Category = "Admin", Description = "Manage roles" },
        };

        context.Permissions.AddRange(permissions);
        await context.SaveChangesAsync();
    }
}
