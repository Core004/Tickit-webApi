using Microsoft.EntityFrameworkCore;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Infrastructure.Persistence.Seeding;

public static class AvatarSeeder
{
    public static async Task SeedAvatarsAsync(ApplicationDbContext context)
    {
        if (await context.Avatars.AnyAsync())
            return;

        var avatars = new List<Avatar>
        {
            // 1. Cartoon (Avataaars style) - FIRST
            new() { Name = "Felix", Url = "https://api.dicebear.com/7.x/avataaars/svg?seed=Felix&backgroundColor=b6e3f4", Category = "Cartoon", DisplayOrder = 1, CreatedAt = DateTime.UtcNow },
            new() { Name = "Aneka", Url = "https://api.dicebear.com/7.x/avataaars/svg?seed=Aneka&backgroundColor=c0aede", Category = "Cartoon", DisplayOrder = 2, CreatedAt = DateTime.UtcNow },
            new() { Name = "Milo", Url = "https://api.dicebear.com/7.x/avataaars/svg?seed=Milo&backgroundColor=d1d4f9", Category = "Cartoon", DisplayOrder = 3, CreatedAt = DateTime.UtcNow },
            new() { Name = "Max", Url = "https://api.dicebear.com/7.x/avataaars/svg?seed=Max&backgroundColor=ffeaa7", Category = "Cartoon", DisplayOrder = 4, CreatedAt = DateTime.UtcNow },
            new() { Name = "Zoe", Url = "https://api.dicebear.com/7.x/avataaars/svg?seed=Zoe&backgroundColor=dfe6e9", Category = "Cartoon", DisplayOrder = 5, CreatedAt = DateTime.UtcNow },
            new() { Name = "Oliver", Url = "https://api.dicebear.com/7.x/avataaars/svg?seed=Oliver&backgroundColor=ffcdd2", Category = "Cartoon", DisplayOrder = 6, CreatedAt = DateTime.UtcNow },
            new() { Name = "Emma", Url = "https://api.dicebear.com/7.x/avataaars/svg?seed=Emma&backgroundColor=e1bee7", Category = "Cartoon", DisplayOrder = 7, CreatedAt = DateTime.UtcNow },
            new() { Name = "Liam", Url = "https://api.dicebear.com/7.x/avataaars/svg?seed=Liam&backgroundColor=bbdefb", Category = "Cartoon", DisplayOrder = 8, CreatedAt = DateTime.UtcNow },

            // 2. Modern (Personas style) - SECOND
            new() { Name = "Alex", Url = "https://api.dicebear.com/7.x/personas/svg?seed=Alex&backgroundColor=d4efdf", Category = "Modern", DisplayOrder = 9, CreatedAt = DateTime.UtcNow },
            new() { Name = "Jordan", Url = "https://api.dicebear.com/7.x/personas/svg?seed=Jordan&backgroundColor=f2d7d5", Category = "Modern", DisplayOrder = 10, CreatedAt = DateTime.UtcNow },
            new() { Name = "Taylor", Url = "https://api.dicebear.com/7.x/personas/svg?seed=Taylor&backgroundColor=d6dbdf", Category = "Modern", DisplayOrder = 11, CreatedAt = DateTime.UtcNow },
            new() { Name = "Morgan", Url = "https://api.dicebear.com/7.x/personas/svg?seed=Morgan&backgroundColor=fae5d3", Category = "Modern", DisplayOrder = 12, CreatedAt = DateTime.UtcNow },

            // 3. Robots (Bottts style) - THIRD
            new() { Name = "Bot-1", Url = "https://api.dicebear.com/7.x/bottts/svg?seed=Robot1&backgroundColor=ffdfbf", Category = "Robots", DisplayOrder = 13, CreatedAt = DateTime.UtcNow },
            new() { Name = "Bot-2", Url = "https://api.dicebear.com/7.x/bottts/svg?seed=Robot2&backgroundColor=aed6f1", Category = "Robots", DisplayOrder = 14, CreatedAt = DateTime.UtcNow },
            new() { Name = "Bot-3", Url = "https://api.dicebear.com/7.x/bottts/svg?seed=Robot3&backgroundColor=d5dbdb", Category = "Robots", DisplayOrder = 15, CreatedAt = DateTime.UtcNow },
            new() { Name = "Bot-4", Url = "https://api.dicebear.com/7.x/bottts/svg?seed=Robot4&backgroundColor=f9e79f", Category = "Robots", DisplayOrder = 16, CreatedAt = DateTime.UtcNow },

            // 4. Fun (Fun emoji style) - FOURTH
            new() { Name = "Fun-1", Url = "https://api.dicebear.com/7.x/fun-emoji/svg?seed=Cat1&backgroundColor=fef9e7", Category = "Fun", DisplayOrder = 17, CreatedAt = DateTime.UtcNow },
            new() { Name = "Fun-2", Url = "https://api.dicebear.com/7.x/fun-emoji/svg?seed=Cat2&backgroundColor=ebf5fb", Category = "Fun", DisplayOrder = 18, CreatedAt = DateTime.UtcNow },
            new() { Name = "Fun-3", Url = "https://api.dicebear.com/7.x/fun-emoji/svg?seed=Cat3&backgroundColor=f5eef8", Category = "Fun", DisplayOrder = 19, CreatedAt = DateTime.UtcNow },
            new() { Name = "Fun-4", Url = "https://api.dicebear.com/7.x/fun-emoji/svg?seed=Cat4&backgroundColor=e8f6f3", Category = "Fun", DisplayOrder = 20, CreatedAt = DateTime.UtcNow },

            // 5. Artistic (Lorelei style)
            new() { Name = "Luna", Url = "https://api.dicebear.com/7.x/lorelei/svg?seed=Luna&backgroundColor=ffd5dc", Category = "Artistic", DisplayOrder = 21, CreatedAt = DateTime.UtcNow },
            new() { Name = "Aria", Url = "https://api.dicebear.com/7.x/lorelei/svg?seed=Aria&backgroundColor=e8daef", Category = "Artistic", DisplayOrder = 22, CreatedAt = DateTime.UtcNow },
            new() { Name = "Nova", Url = "https://api.dicebear.com/7.x/lorelei/svg?seed=Nova&backgroundColor=d5f5e3", Category = "Artistic", DisplayOrder = 23, CreatedAt = DateTime.UtcNow },
            new() { Name = "Sage", Url = "https://api.dicebear.com/7.x/lorelei/svg?seed=Sage&backgroundColor=fdebd0", Category = "Artistic", DisplayOrder = 24, CreatedAt = DateTime.UtcNow },

            // 6. Professional (Notionists style)
            new() { Name = "Pro-1", Url = "https://api.dicebear.com/7.x/notionists/svg?seed=Pro1&backgroundColor=c1e1c5", Category = "Professional", DisplayOrder = 25, CreatedAt = DateTime.UtcNow },
            new() { Name = "Pro-2", Url = "https://api.dicebear.com/7.x/notionists/svg?seed=Pro2&backgroundColor=fadbd8", Category = "Professional", DisplayOrder = 26, CreatedAt = DateTime.UtcNow },
            new() { Name = "Pro-3", Url = "https://api.dicebear.com/7.x/notionists/svg?seed=Pro3&backgroundColor=d6eaf8", Category = "Professional", DisplayOrder = 27, CreatedAt = DateTime.UtcNow },
            new() { Name = "Pro-4", Url = "https://api.dicebear.com/7.x/notionists/svg?seed=Pro4&backgroundColor=fcf3cf", Category = "Professional", DisplayOrder = 28, CreatedAt = DateTime.UtcNow },

            // 7. Adventure (Adventurer style)
            new() { Name = "Hero-1", Url = "https://api.dicebear.com/7.x/adventurer/svg?seed=Hero1&backgroundColor=abebc6", Category = "Adventure", DisplayOrder = 29, CreatedAt = DateTime.UtcNow },
            new() { Name = "Hero-2", Url = "https://api.dicebear.com/7.x/adventurer/svg?seed=Hero2&backgroundColor=f5b7b1", Category = "Adventure", DisplayOrder = 30, CreatedAt = DateTime.UtcNow },
            new() { Name = "Hero-3", Url = "https://api.dicebear.com/7.x/adventurer/svg?seed=Hero3&backgroundColor=a9cce3", Category = "Adventure", DisplayOrder = 31, CreatedAt = DateTime.UtcNow },
            new() { Name = "Hero-4", Url = "https://api.dicebear.com/7.x/adventurer/svg?seed=Hero4&backgroundColor=f9e79f", Category = "Adventure", DisplayOrder = 32, CreatedAt = DateTime.UtcNow },
        };

        context.Avatars.AddRange(avatars);
        await context.SaveChangesAsync();
    }
}
