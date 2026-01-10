using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Application.Common.Models;
using TicketSystem.Domain.Entities;

namespace TicketSystem.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IApplicationDbContext context,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedList<UserListDto>>> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u =>
                u.Email!.Contains(search) ||
                u.FirstName.Contains(search) ||
                u.LastName.Contains(search));
        }

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        query = query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName);

        var totalCount = await query.CountAsync();
        var items = await query
            .Include(u => u.Company)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserListDto
            {
                Id = u.Id,
                Email = u.Email!,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive,
                CompanyId = u.CompanyId,
                CompanyName = u.Company != null ? u.Company.Name : null,
                DepartmentId = u.DepartmentId,
                DepartmentName = _context.Departments.Where(d => d.Id == u.DepartmentId).Select(d => d.Name).FirstOrDefault(),
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync();

        // Get roles for each user
        foreach (var user in items)
        {
            var appUser = await _userManager.FindByIdAsync(user.Id);
            if (appUser != null)
                user.Roles = (await _userManager.GetRolesAsync(appUser)).ToList();
        }

        return Ok(new PaginatedList<UserListDto>(items, totalCount, pageNumber, pageSize));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDetailDto>> GetUser(string id)
    {
        var user = await _userManager.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var departmentName = user.DepartmentId.HasValue
            ? await _context.Departments.Where(d => d.Id == user.DepartmentId).Select(d => d.Name).FirstOrDefaultAsync()
            : null;

        return Ok(new UserDetailDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            ProfilePicture = user.ProfilePicture,
            IsActive = user.IsActive,
            CompanyId = user.CompanyId,
            CompanyName = user.Company?.Name,
            DepartmentId = user.DepartmentId,
            DepartmentName = departmentName,
            Roles = roles.ToList(),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            LastLoginAt = user.LastLoginAt
        });
    }

    [HttpPost]
    public async Task<ActionResult<string>> CreateUser([FromBody] CreateUserRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            return BadRequest(new { Message = "User with this email already exists" });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            CompanyId = request.CompanyId,
            DepartmentId = request.DepartmentId,
            ProfilePicture = request.ProfilePicture,
            IsActive = true,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });

        if (request.Roles?.Any() == true)
        {
            foreach (var role in request.Roles)
            {
                if (await _roleManager.RoleExistsAsync(role))
                    await _userManager.AddToRoleAsync(user, role);
            }
        }
        else
        {
            await _userManager.AddToRoleAsync(user, "Customer");
        }

        _logger.LogInformation("User {Email} created", user.Email);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user.Id);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;
        user.CompanyId = request.CompanyId;
        user.DepartmentId = request.DepartmentId;
        user.ProfilePicture = request.ProfilePicture;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });

        // Update roles
        if (request.Roles != null)
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            foreach (var role in request.Roles)
            {
                if (await _roleManager.RoleExistsAsync(role))
                    await _userManager.AddToRoleAsync(user, role);
            }
        }

        return NoContent();
    }

    [HttpPost("{id}/activate")]
    public async Task<IActionResult> ActivateUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return NoContent();
    }

    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> DeactivateUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        // Remove user permissions first
        var userPermissions = await _context.UserPermissions
            .Where(up => up.UserId == id)
            .ToListAsync();
        foreach (var up in userPermissions)
            _context.UserPermissions.Remove(up);
        await _context.SaveChangesAsync();

        // Delete the user
        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });

        _logger.LogInformation("User {Email} deleted permanently", user.Email);

        return NoContent();
    }

    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(string id, [FromBody] ResetPasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

        if (!result.Succeeded)
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });

        return NoContent();
    }

    [HttpGet("{id}/permissions")]
    public async Task<ActionResult<UserPermissionsDto>> GetUserPermissions(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        // Get user's roles
        var roles = await _userManager.GetRolesAsync(user);

        // Get role-based permissions
        var rolePermissions = await _context.RolePermissions
            .Include(rp => rp.Permission)
            .Where(rp => roles.Contains(rp.RoleId))
            .Select(rp => new UserPermissionItemDto
            {
                Id = rp.Permission.Id,
                Name = rp.Permission.Name,
                Description = rp.Permission.Description,
                Category = rp.Permission.Category,
                Source = "Role"
            })
            .ToListAsync();

        // Get direct user permissions
        var userPermissions = await _context.UserPermissions
            .Include(up => up.Permission)
            .Where(up => up.UserId == id)
            .Select(up => new UserPermissionItemDto
            {
                Id = up.Permission.Id,
                Name = up.Permission.Name,
                Description = up.Permission.Description,
                Category = up.Permission.Category,
                Source = up.IsGranted ? "Granted" : "Denied"
            })
            .ToListAsync();

        // Combine and deduplicate (user permissions override role permissions)
        var allPermissions = new Dictionary<int, UserPermissionItemDto>();

        foreach (var rp in rolePermissions)
            allPermissions[rp.Id] = rp;

        foreach (var up in userPermissions)
            allPermissions[up.Id] = up;

        return Ok(new UserPermissionsDto
        {
            UserId = id,
            Roles = roles.ToList(),
            Permissions = allPermissions.Values.OrderBy(p => p.Category).ThenBy(p => p.Name).ToList()
        });
    }

    [HttpPut("{id}/permissions")]
    public async Task<IActionResult> UpdateUserPermissions(string id, [FromBody] UpdateUserPermissionsRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        // Remove existing user permissions
        var existingPermissions = await _context.UserPermissions
            .Where(up => up.UserId == id)
            .ToListAsync();

        foreach (var ep in existingPermissions)
        {
            _context.UserPermissions.Remove(ep);
        }

        // Add granted permissions
        if (request.GrantedPermissionIds?.Any() == true)
        {
            foreach (var permissionId in request.GrantedPermissionIds)
            {
                var permission = await _context.Permissions.FindAsync(permissionId);
                if (permission != null)
                {
                    _context.UserPermissions.Add(new UserPermission
                    {
                        UserId = id,
                        PermissionId = permissionId,
                        IsGranted = true
                    });
                }
            }
        }

        // Add denied permissions (explicit denials override role permissions)
        if (request.DeniedPermissionIds?.Any() == true)
        {
            foreach (var permissionId in request.DeniedPermissionIds)
            {
                var permission = await _context.Permissions.FindAsync(permissionId);
                if (permission != null)
                {
                    _context.UserPermissions.Add(new UserPermission
                    {
                        UserId = id,
                        PermissionId = permissionId,
                        IsGranted = false
                    });
                }
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Permissions updated for user {UserId}", id);

        return NoContent();
    }
}

// DTOs
public class UserListDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class UserDetailDto : UserListDto
{
    public string? PhoneNumber { get; set; }
    public string? ProfilePicture { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public record CreateUserRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    int? CompanyId,
    int? DepartmentId,
    List<string>? Roles,
    string? ProfilePicture);

public record UpdateUserRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    int? CompanyId,
    int? DepartmentId,
    List<string>? Roles,
    string? ProfilePicture);

public record ResetPasswordRequest(string NewPassword);

public class UserPermissionsDto
{
    public string UserId { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public List<UserPermissionItemDto> Permissions { get; set; } = new();
}

public class UserPermissionItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string Source { get; set; } = string.Empty; // "Role", "Granted", or "Denied"
}

public record UpdateUserPermissionsRequest(
    List<int>? GrantedPermissionIds,
    List<int>? DeniedPermissionIds);
