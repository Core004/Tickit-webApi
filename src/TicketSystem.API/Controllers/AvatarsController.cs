using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Domain.Entities;

namespace TicketSystem.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class AvatarsController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public AvatarsController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<AvatarDto>>> GetAvatars([FromQuery] bool? isActive = true)
    {
        var query = _context.Avatars.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(a => a.IsActive == isActive.Value);

        var avatars = await query
            .OrderBy(a => a.DisplayOrder)
            .Select(a => new AvatarDto
            {
                Id = a.Id,
                Name = a.Name,
                Url = a.Url,
                Category = a.Category,
                DisplayOrder = a.DisplayOrder,
                IsActive = a.IsActive
            })
            .ToListAsync();

        return Ok(avatars);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AvatarDto>> GetAvatar(int id)
    {
        var avatar = await _context.Avatars.FindAsync(id);

        if (avatar is null)
            return NotFound();

        return Ok(new AvatarDto
        {
            Id = avatar.Id,
            Name = avatar.Name,
            Url = avatar.Url,
            Category = avatar.Category,
            DisplayOrder = avatar.DisplayOrder,
            IsActive = avatar.IsActive
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<int>> CreateAvatar([FromBody] CreateAvatarRequest request)
    {
        var avatar = new Avatar
        {
            Name = request.Name,
            Url = request.Url,
            Category = request.Category,
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Avatars.Add(avatar);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAvatar), new { id = avatar.Id }, avatar.Id);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateAvatar(int id, [FromBody] UpdateAvatarRequest request)
    {
        var avatar = await _context.Avatars.FindAsync(id);

        if (avatar is null)
            return NotFound();

        avatar.Name = request.Name;
        avatar.Url = request.Url;
        avatar.Category = request.Category;
        avatar.DisplayOrder = request.DisplayOrder;
        avatar.IsActive = request.IsActive;
        avatar.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAvatar(int id)
    {
        var avatar = await _context.Avatars.FindAsync(id);

        if (avatar is null)
            return NotFound();

        _context.Avatars.Remove(avatar);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

// DTOs
public class AvatarDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public record CreateAvatarRequest(
    string Name,
    string Url,
    string? Category,
    int DisplayOrder = 0,
    bool IsActive = true);

public record UpdateAvatarRequest(
    string Name,
    string Url,
    string? Category,
    int DisplayOrder,
    bool IsActive);
