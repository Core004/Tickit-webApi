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
public class TeamsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<TeamsController> _logger;

    public TeamsController(IApplicationDbContext context, ILogger<TeamsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedList<TeamDto>>> GetTeams(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] int? departmentId = null)
    {
        var query = _context.Teams
            .Include(t => t.Department)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(t => t.Name.Contains(search));

        if (departmentId.HasValue)
            query = query.Where(t => t.DepartmentId == departmentId.Value);

        query = query.OrderBy(t => t.Name);

        var result = await PaginatedList<TeamDto>.CreateAsync(
            query.Select(t => new TeamDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                DepartmentId = t.DepartmentId,
                DepartmentName = t.Department != null ? t.Department.Name : null,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt
            }),
            pageNumber, pageSize);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TeamDetailDto>> GetTeam(int id)
    {
        var team = await _context.Teams
            .Include(t => t.Department)
            .Include(t => t.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (team is null)
            return NotFound();

        // Get employee info for team members
        var activeMembers = team.Members.Where(m => m.IsActive).ToList();
        var userIds = activeMembers.Select(m => m.UserId).ToList();
        var employees = await _context.Employees
            .Where(e => e.UserId != null && userIds.Contains(e.UserId))
            .ToDictionaryAsync(e => e.UserId!, e => e);

        return Ok(new TeamDetailDto
        {
            Id = team.Id,
            Name = team.Name,
            Description = team.Description,
            DepartmentId = team.DepartmentId,
            DepartmentName = team.Department?.Name,
            IsActive = team.IsActive,
            CreatedAt = team.CreatedAt,
            UpdatedAt = team.UpdatedAt,
            Members = activeMembers.Select(m =>
            {
                var employee = employees.GetValueOrDefault(m.UserId);
                return new TeamMemberDto
                {
                    Id = m.Id,
                    TeamId = m.TeamId,
                    UserId = m.UserId,
                    UserName = m.User?.FullName,
                    UserEmail = m.User?.Email,
                    MemberRole = m.IsLead ? "Lead" : "Agent",
                    JoinedAt = m.JoinedAt,
                    IsActive = m.IsActive,
                    EmployeeId = employee?.Id,
                    EmployeeName = employee?.Name,
                    EmployeeCode = employee?.EmployeeCode
                };
            }).ToList()
        });
    }

    [HttpPost]
    public async Task<ActionResult<int>> CreateTeam([FromBody] CreateTeamRequest request)
    {
        var team = new Team
        {
            Name = request.Name,
            Description = request.Description,
            DepartmentId = request.DepartmentId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Team {Name} created with ID {Id}", team.Name, team.Id);

        return CreatedAtAction(nameof(GetTeam), new { id = team.Id }, team.Id);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTeam(int id, [FromBody] UpdateTeamRequest request)
    {
        var team = await _context.Teams.FindAsync(id);
        if (team is null)
            return NotFound();

        team.Name = request.Name;
        team.Description = request.Description;
        team.DepartmentId = request.DepartmentId;
        team.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id}/members")]
    public async Task<ActionResult<List<TeamMemberDto>>> GetMembers(int id)
    {
        var members = await _context.TeamMembers
            .Include(m => m.User)
            .Where(m => m.TeamId == id && m.IsActive)
            .ToListAsync();

        // Get employee info for each member
        var userIds = members.Select(m => m.UserId).ToList();
        var employees = await _context.Employees
            .Where(e => e.UserId != null && userIds.Contains(e.UserId))
            .ToDictionaryAsync(e => e.UserId!, e => e);

        var result = members.Select(m =>
        {
            var employee = employees.GetValueOrDefault(m.UserId);
            return new TeamMemberDto
            {
                Id = m.Id,
                TeamId = m.TeamId,
                UserId = m.UserId,
                UserName = m.User?.FullName,
                UserEmail = m.User?.Email,
                MemberRole = m.IsLead ? "Lead" : "Agent",
                JoinedAt = m.JoinedAt,
                IsActive = m.IsActive,
                EmployeeId = employee?.Id,
                EmployeeName = employee?.Name,
                EmployeeCode = employee?.EmployeeCode
            };
        }).ToList();

        return Ok(result);
    }

    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddMember(int id, [FromBody] AddTeamMemberRequest request)
    {
        var team = await _context.Teams.FindAsync(id);
        if (team is null)
            return NotFound();

        var existingMember = await _context.TeamMembers
            .FirstOrDefaultAsync(m => m.TeamId == id && m.UserId == request.UserId);

        if (existingMember != null)
        {
            // Reactivate if previously removed
            if (!existingMember.IsActive)
            {
                existingMember.IsActive = true;
                existingMember.IsLead = request.MemberRole.Equals("Lead", StringComparison.OrdinalIgnoreCase);
                existingMember.JoinedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(existingMember.Id);
            }
            return BadRequest(new { Message = "User is already a member of this team" });
        }

        var member = new TeamMember
        {
            TeamId = id,
            UserId = request.UserId,
            IsLead = request.MemberRole.Equals("Lead", StringComparison.OrdinalIgnoreCase),
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        };

        _context.TeamMembers.Add(member);
        await _context.SaveChangesAsync();

        return Ok(member.Id);
    }

    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(int id, string userId)
    {
        var member = await _context.TeamMembers
            .FirstOrDefaultAsync(m => m.TeamId == id && m.UserId == userId && m.IsActive);

        if (member is null)
            return NotFound();

        // Soft delete
        member.IsActive = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTeam(int id)
    {
        var team = await _context.Teams.FindAsync(id);
        if (team is null)
            return NotFound();

        _context.Teams.Remove(team);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

// DTOs
public class TeamDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TeamDetailDto : TeamDto
{
    public DateTime? UpdatedAt { get; set; }
    public List<TeamMemberDto> Members { get; set; } = new();
}

public class TeamMemberDto
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string MemberRole { get; set; } = "Agent";
    public DateTime JoinedAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Employee details
    public int? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? EmployeeCode { get; set; }
}

public record CreateTeamRequest(string Name, string? Description, int? DepartmentId);
public record UpdateTeamRequest(string Name, string? Description, int? DepartmentId);
public record AddTeamMemberRequest(string UserId, string MemberRole = "Agent");
