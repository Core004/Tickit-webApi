using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Infrastructure.Persistence.Seeding;

public static class AdminUserSeeder
{
    public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        const string adminEmail = "admin@gmail.com";
        const string adminPassword = "Admin@123";

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin is not null)
        {
            // Reset password if user exists
            var token = await userManager.GeneratePasswordResetTokenAsync(existingAdmin);
            await userManager.ResetPasswordAsync(existingAdmin, token, adminPassword);
            return;
        }

        var adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "System",
            LastName = "Admin",
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}
