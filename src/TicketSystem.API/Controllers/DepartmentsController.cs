using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Application.Common.Models;
using TicketSystem.Domain.Entities;

namespace TicketSystem.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class DepartmentsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<DepartmentsController> _logger;

    public DepartmentsController(IApplicationDbContext context, ILogger<DepartmentsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedList<DepartmentDto>>> GetDepartments(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] int? companyId = null)
    {
        var query = _context.Departments
            .Include(d => d.Company)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(d => d.Name.Contains(search));

        if (companyId.HasValue)
            query = query.Where(d => d.CompanyId == companyId.Value);

        query = query.OrderBy(d => d.Name);

        var result = await PaginatedList<DepartmentDto>.CreateAsync(
            query.Select(d => new DepartmentDto
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                CompanyId = d.CompanyId,
                CompanyName = d.Company != null ? d.Company.Name : null,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt
            }),
            pageNumber, pageSize);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DepartmentDetailDto>> GetDepartment(int id)
    {
        var department = await _context.Departments
            .Include(d => d.Company)
            .Include(d => d.Members)
                .ThenInclude(m => m.User)
            .Include(d => d.Teams)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department is null)
            return NotFound();

        return Ok(new DepartmentDetailDto
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
            CompanyId = department.CompanyId,
            CompanyName = department.Company?.Name,
            IsActive = department.IsActive,
            CreatedAt = department.CreatedAt,
            UpdatedAt = department.UpdatedAt,
            Members = department.Members.Select(m => new DepartmentMemberDto
            {
                UserId = m.UserId,
                UserName = m.User.FullName,
                Email = m.User.Email!,
                IsManager = m.IsManager
            }).ToList(),
            TeamCount = department.Teams.Count
        });
    }

    [HttpPost]
    public async Task<ActionResult<int>> CreateDepartment([FromBody] CreateDepartmentRequest request)
    {
        var department = new Department
        {
            Name = request.Name,
            Description = request.Description,
            CompanyId = request.CompanyId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Department {Name} created with ID {Id}", department.Name, department.Id);

        return CreatedAtAction(nameof(GetDepartment), new { id = department.Id }, department.Id);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDepartment(int id, [FromBody] UpdateDepartmentRequest request)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department is null)
            return NotFound();

        department.Name = request.Name;
        department.Description = request.Description;
        department.CompanyId = request.CompanyId;
        department.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id}/members")]
    public async Task<ActionResult<List<DepartmentMemberDto>>> GetMembers(int id)
    {
        var members = await _context.DepartmentMembers
            .Include(m => m.User)
            .Where(m => m.DepartmentId == id)
            .Select(m => new DepartmentMemberDto
            {
                Id = m.Id,
                UserId = m.UserId,
                UserName = m.User != null ? m.User.FullName : null,
                Email = m.User != null ? m.User.Email : null,
                IsManager = m.IsManager,
                JoinedAt = m.JoinedAt
            })
            .ToListAsync();

        return Ok(members);
    }

    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddMember(int id, [FromBody] AddDepartmentMemberRequest request)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department is null)
            return NotFound();

        var existingMember = await _context.DepartmentMembers
            .FirstOrDefaultAsync(m => m.DepartmentId == id && m.UserId == request.UserId);

        if (existingMember != null)
            return BadRequest(new { Message = "User is already a member of this department" });

        var member = new DepartmentMember
        {
            DepartmentId = id,
            UserId = request.UserId,
            IsManager = request.IsManager,
            JoinedAt = DateTime.UtcNow
        };

        _context.DepartmentMembers.Add(member);
        await _context.SaveChangesAsync();

        return Ok(member.Id);
    }

    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(int id, string userId)
    {
        var member = await _context.DepartmentMembers
            .FirstOrDefaultAsync(m => m.DepartmentId == id && m.UserId == userId);

        if (member is null)
            return NotFound();

        _context.DepartmentMembers.Remove(member);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department is null)
            return NotFound();

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

// DTOs
public class DepartmentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DepartmentDetailDto : DepartmentDto
{
    public DateTime? UpdatedAt { get; set; }
    public List<DepartmentMemberDto> Members { get; set; } = new();
    public int TeamCount { get; set; }
}

public class DepartmentMemberDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public bool IsManager { get; set; }
    public DateTime JoinedAt { get; set; }
}

public record CreateDepartmentRequest(string Name, string? Description, int? CompanyId);
public record UpdateDepartmentRequest(string Name, string? Description, int? CompanyId);
public record AddDepartmentMemberRequest(string UserId, bool IsManager = false);
