using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Domain.Entities;

namespace TicketSystem.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class RolesController : ControllerBase
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<RolesController> _logger;

    public RolesController(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext context,
        ILogger<RolesController> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<RoleDto>>> GetRoles()
    {
        var roles = await _roleManager.Roles
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name!
            })
            .ToListAsync();

        // Get user count for each role
        foreach (var role in roles)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
            role.UserCount = usersInRole.Count;
        }

        // Get permissions for each role
        foreach (var role in roles)
        {
            var rolePermissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .CountAsync();
            role.PermissionCount = rolePermissions;
        }

        return Ok(roles);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RoleDetailDto>> GetRole(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
            return NotFound();

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
        var permissions = await _context.RolePermissions
            .Include(rp => rp.Permission)
            .Where(rp => rp.RoleId == id)
            .Select(rp => new PermissionDto
            {
                Id = rp.Permission.Id,
                Name = rp.Permission.Name,
                Description = rp.Permission.Description,
                Category = rp.Permission.Category
            })
            .ToListAsync();

        return Ok(new RoleDetailDto
        {
            Id = role.Id,
            Name = role.Name!,
            UserCount = usersInRole.Count,
            PermissionCount = permissions.Count,
            Permissions = permissions,
            Users = usersInRole.Select(u => new RoleUserDto
            {
                Id = u.Id,
                Email = u.Email!,
                FullName = u.FullName
            }).ToList()
        });
    }

    [HttpPost]
    public async Task<ActionResult<string>> CreateRole([FromBody] CreateRoleRequest request)
    {
        if (await _roleManager.RoleExistsAsync(request.Name))
            return BadRequest(new { Message = "Role already exists" });

        var role = new IdentityRole(request.Name);
        var result = await _roleManager.CreateAsync(role);

        if (!result.Succeeded)
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });

        _logger.LogInformation("Role {Name} created", request.Name);

        return CreatedAtAction(nameof(GetRole), new { id = role.Id }, role.Id);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] UpdateRoleRequest request)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
            return NotFound();

        // Prevent renaming system roles
        if (role.Name is "Admin" or "Agent" or "Customer")
            return BadRequest(new { Message = "Cannot rename system roles" });

        role.Name = request.Name;
        role.NormalizedName = request.Name.ToUpper();

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });

        return NoContent();
    }

    [HttpPost("{id}/permissions")]
    public async Task<IActionResult> AssignPermissions(string id, [FromBody] AssignPermissionsRequest request)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
            return NotFound();

        // Remove existing permissions
        var existingPermissions = await _context.RolePermissions
            .Where(rp => rp.RoleId == id)
            .ToListAsync();
        _context.RolePermissions.RemoveRange(existingPermissions);

        // Add new permissions
        foreach (var permissionId in request.PermissionIds)
        {
            var permission = await _context.Permissions.FindAsync(permissionId);
            if (permission != null)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = id,
                    PermissionId = permissionId
                });
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Permissions updated for role {RoleId}", id);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
            return NotFound();

        // Prevent deleting system roles
        if (role.Name is "Admin" or "Agent" or "Customer")
            return BadRequest(new { Message = "Cannot delete system roles" });

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
        if (usersInRole.Any())
            return BadRequest(new { Message = "Cannot delete role with assigned users" });

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });

        return NoContent();
    }
}

// DTOs
public class RoleDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int UserCount { get; set; }
    public int PermissionCount { get; set; }
}

public class RoleDetailDto : RoleDto
{
    public List<PermissionDto> Permissions { get; set; } = new();
    public List<RoleUserDto> Users { get; set; } = new();
}

public class RoleUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public class PermissionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
}

public record CreateRoleRequest(string Name);
public record UpdateRoleRequest(string Name);
public record AssignPermissionsRequest(List<int> PermissionIds);
