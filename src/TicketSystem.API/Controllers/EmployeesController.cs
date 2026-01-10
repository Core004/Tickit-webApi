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
public class EmployeesController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(IApplicationDbContext context, ILogger<EmployeesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedList<EmployeeDto>>> GetEmployees(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] int? companyId = null,
        [FromQuery] int? departmentId = null,
        [FromQuery] int? teamId = null,
        [FromQuery] bool? isActive = null)
    {
        var query = _context.Employees
            .Include(e => e.Company)
            .Include(e => e.Department)
            .Include(e => e.Team)
            .Include(e => e.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(e =>
                e.Name.Contains(search) ||
                e.Email.Contains(search) ||
                e.EmployeeCode.Contains(search));

        if (companyId.HasValue)
            query = query.Where(e => e.CompanyId == companyId.Value);

        if (departmentId.HasValue)
            query = query.Where(e => e.DepartmentId == departmentId.Value);

        if (teamId.HasValue)
            query = query.Where(e => e.TeamId == teamId.Value);

        if (isActive.HasValue)
            query = query.Where(e => e.IsActive == isActive.Value);

        query = query.OrderBy(e => e.Name);

        var result = await PaginatedList<EmployeeDto>.CreateAsync(
            query.Select(e => new EmployeeDto
            {
                Id = e.Id,
                Name = e.Name,
                Email = e.Email,
                Phone = e.Phone,
                EmployeeCode = e.EmployeeCode,
                JoinDate = e.JoinDate,
                IsActive = e.IsActive,
                UserId = e.UserId,
                HasLinkedUser = e.UserId != null,
                CompanyId = e.CompanyId,
                CompanyName = e.Company != null ? e.Company.Name : null,
                DepartmentId = e.DepartmentId,
                DepartmentName = e.Department != null ? e.Department.Name : null,
                TeamId = e.TeamId,
                TeamName = e.Team != null ? e.Team.Name : null,
                CreatedAt = e.CreatedAt
            }),
            pageNumber, pageSize);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EmployeeDetailDto>> GetEmployee(int id)
    {
        var employee = await _context.Employees
            .Include(e => e.Company)
            .Include(e => e.Department)
            .Include(e => e.Team)
            .Include(e => e.User)
            .Include(e => e.ReportingPersons)
                .ThenInclude(rp => rp.ReportingPerson)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee is null)
            return NotFound();

        return Ok(new EmployeeDetailDto
        {
            Id = employee.Id,
            Name = employee.Name,
            Email = employee.Email,
            Phone = employee.Phone,
            EmployeeCode = employee.EmployeeCode,
            JoinDate = employee.JoinDate,
            IsActive = employee.IsActive,
            UserId = employee.UserId,
            HasLinkedUser = employee.UserId != null,
            CompanyId = employee.CompanyId,
            CompanyName = employee.Company?.Name,
            DepartmentId = employee.DepartmentId,
            DepartmentName = employee.Department?.Name,
            TeamId = employee.TeamId,
            TeamName = employee.Team?.Name,
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt,
            ReportingPersons = employee.ReportingPersons
                .Where(rp => rp.IsActive)
                .Select(rp => new EmployeeReportingPersonDto
                {
                    Id = rp.Id,
                    ReportingPersonId = rp.ReportingPersonId,
                    ReportingPersonName = rp.ReportingPerson.Name,
                    ReportingPersonEmail = rp.ReportingPerson.Email,
                    IsPrimary = rp.IsPrimary,
                    AssignedAt = rp.AssignedAt
                }).ToList()
        });
    }

    [HttpPost]
    public async Task<ActionResult<int>> CreateEmployee([FromBody] CreateEmployeeRequest request)
    {
        // Check for duplicate employee code
        var existingCode = await _context.Employees
            .AnyAsync(e => e.EmployeeCode == request.EmployeeCode);
        if (existingCode)
            return BadRequest(new { Message = "Employee code already exists" });

        var employee = new Employee
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            EmployeeCode = request.EmployeeCode,
            JoinDate = request.JoinDate,
            IsActive = true,
            UserId = request.UserId,
            CompanyId = request.CompanyId,
            DepartmentId = request.DepartmentId,
            TeamId = request.TeamId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        // Add reporting persons if provided
        if (request.ReportingPersonIds != null && request.ReportingPersonIds.Any())
        {
            var isFirst = true;
            foreach (var rpId in request.ReportingPersonIds)
            {
                _context.EmployeeReportingPersons.Add(new EmployeeReportingPerson
                {
                    EmployeeId = employee.Id,
                    ReportingPersonId = rpId,
                    IsPrimary = isFirst,
                    AssignedAt = DateTime.UtcNow,
                    IsActive = true
                });
                isFirst = false;
            }
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Employee {Name} created with ID {Id}", employee.Name, employee.Id);

        return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee.Id);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeRequest request)
    {
        var employee = await _context.Employees
            .Include(e => e.ReportingPersons)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (employee is null)
            return NotFound();

        // Check for duplicate employee code (excluding current employee)
        if (request.EmployeeCode != employee.EmployeeCode)
        {
            var existingCode = await _context.Employees
                .AnyAsync(e => e.EmployeeCode == request.EmployeeCode && e.Id != id);
            if (existingCode)
                return BadRequest(new { Message = "Employee code already exists" });
        }

        employee.Name = request.Name;
        employee.Email = request.Email;
        employee.Phone = request.Phone;
        employee.EmployeeCode = request.EmployeeCode;
        employee.JoinDate = request.JoinDate;
        if (request.IsActive.HasValue)
            employee.IsActive = request.IsActive.Value;
        employee.UserId = request.UserId;
        employee.CompanyId = request.CompanyId;
        employee.DepartmentId = request.DepartmentId;
        employee.TeamId = request.TeamId;
        employee.UpdatedAt = DateTime.UtcNow;

        // Update reporting persons if provided
        if (request.ReportingPersonIds != null)
        {
            var existingRpIds = employee.ReportingPersons.Where(rp => rp.IsActive).Select(rp => rp.ReportingPersonId).ToList();
            var newRpIds = request.ReportingPersonIds;

            // Remove old associations
            var toRemove = employee.ReportingPersons.Where(rp => !newRpIds.Contains(rp.ReportingPersonId)).ToList();
            foreach (var rp in toRemove)
            {
                rp.IsActive = false;
            }

            // Add new associations
            var toAdd = newRpIds.Where(rpId => !existingRpIds.Contains(rpId)).ToList();
            var isFirst = !newRpIds.Any(rpId => existingRpIds.Contains(rpId));
            foreach (var rpId in toAdd)
            {
                _context.EmployeeReportingPersons.Add(new EmployeeReportingPerson
                {
                    EmployeeId = id,
                    ReportingPersonId = rpId,
                    IsPrimary = isFirst,
                    AssignedAt = DateTime.UtcNow,
                    IsActive = true
                });
                isFirst = false;
            }
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee is null)
            return NotFound();

        _context.Employees.Remove(employee);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Reporting Person Management

    [HttpGet("{id}/reporting-persons")]
    public async Task<ActionResult<List<EmployeeReportingPersonDto>>> GetReportingPersons(int id)
    {
        var reportingPersons = await _context.EmployeeReportingPersons
            .Include(rp => rp.ReportingPerson)
            .Where(rp => rp.EmployeeId == id && rp.IsActive)
            .Select(rp => new EmployeeReportingPersonDto
            {
                Id = rp.Id,
                ReportingPersonId = rp.ReportingPersonId,
                ReportingPersonName = rp.ReportingPerson.Name,
                ReportingPersonEmail = rp.ReportingPerson.Email,
                IsPrimary = rp.IsPrimary,
                AssignedAt = rp.AssignedAt
            })
            .ToListAsync();

        return Ok(reportingPersons);
    }

    [HttpPost("{id}/reporting-persons")]
    public async Task<IActionResult> AddReportingPerson(int id, [FromBody] AddReportingPersonRequest request)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee is null)
            return NotFound();

        var existingRp = await _context.EmployeeReportingPersons
            .FirstOrDefaultAsync(rp => rp.EmployeeId == id && rp.ReportingPersonId == request.ReportingPersonId && rp.IsActive);

        if (existingRp != null)
            return BadRequest(new { Message = "This reporting person is already assigned" });

        // Prevent self-assignment
        if (request.ReportingPersonId == id)
            return BadRequest(new { Message = "An employee cannot report to themselves" });

        var rp = new EmployeeReportingPerson
        {
            EmployeeId = id,
            ReportingPersonId = request.ReportingPersonId,
            IsPrimary = request.IsPrimary,
            AssignedAt = DateTime.UtcNow,
            IsActive = true
        };

        // If setting as primary, unset other primaries
        if (request.IsPrimary)
        {
            var existingPrimaries = await _context.EmployeeReportingPersons
                .Where(erp => erp.EmployeeId == id && erp.IsPrimary && erp.IsActive)
                .ToListAsync();
            foreach (var primary in existingPrimaries)
            {
                primary.IsPrimary = false;
            }
        }

        _context.EmployeeReportingPersons.Add(rp);
        await _context.SaveChangesAsync();

        return Ok(rp.Id);
    }

    [HttpDelete("{id}/reporting-persons/{rpId}")]
    public async Task<IActionResult> RemoveReportingPerson(int id, int rpId)
    {
        var rp = await _context.EmployeeReportingPersons
            .FirstOrDefaultAsync(r => r.Id == rpId && r.EmployeeId == id);

        if (rp is null)
            return NotFound();

        rp.IsActive = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}/reporting-persons/{rpId}/set-primary")]
    public async Task<IActionResult> SetPrimaryReportingPerson(int id, int rpId)
    {
        var rp = await _context.EmployeeReportingPersons
            .FirstOrDefaultAsync(r => r.Id == rpId && r.EmployeeId == id && r.IsActive);

        if (rp is null)
            return NotFound();

        // Unset other primaries
        var existingPrimaries = await _context.EmployeeReportingPersons
            .Where(erp => erp.EmployeeId == id && erp.IsPrimary && erp.IsActive && erp.Id != rpId)
            .ToListAsync();
        foreach (var primary in existingPrimaries)
        {
            primary.IsPrimary = false;
        }

        rp.IsPrimary = true;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id}/subordinates")]
    public async Task<ActionResult<List<EmployeeDto>>> GetSubordinates(int id)
    {
        var subordinates = await _context.EmployeeReportingPersons
            .Include(rp => rp.Employee)
                .ThenInclude(e => e.Company)
            .Include(rp => rp.Employee)
                .ThenInclude(e => e.Department)
            .Include(rp => rp.Employee)
                .ThenInclude(e => e.Team)
            .Where(rp => rp.ReportingPersonId == id && rp.IsActive)
            .Select(rp => new EmployeeDto
            {
                Id = rp.Employee.Id,
                Name = rp.Employee.Name,
                Email = rp.Employee.Email,
                Phone = rp.Employee.Phone,
                EmployeeCode = rp.Employee.EmployeeCode,
                JoinDate = rp.Employee.JoinDate,
                IsActive = rp.Employee.IsActive,
                UserId = rp.Employee.UserId,
                HasLinkedUser = rp.Employee.UserId != null,
                CompanyId = rp.Employee.CompanyId,
                CompanyName = rp.Employee.Company != null ? rp.Employee.Company.Name : null,
                DepartmentId = rp.Employee.DepartmentId,
                DepartmentName = rp.Employee.Department != null ? rp.Employee.Department.Name : null,
                TeamId = rp.Employee.TeamId,
                TeamName = rp.Employee.Team != null ? rp.Employee.Team.Name : null,
                CreatedAt = rp.Employee.CreatedAt
            })
            .ToListAsync();

        return Ok(subordinates);
    }
}

// DTOs
public class EmployeeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public DateTime JoinDate { get; set; }
    public bool IsActive { get; set; }
    public string? UserId { get; set; }
    public bool HasLinkedUser { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int? TeamId { get; set; }
    public string? TeamName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class EmployeeDetailDto : EmployeeDto
{
    public DateTime? UpdatedAt { get; set; }
    public List<EmployeeReportingPersonDto> ReportingPersons { get; set; } = new();
}

public class EmployeeReportingPersonDto
{
    public int Id { get; set; }
    public int ReportingPersonId { get; set; }
    public string ReportingPersonName { get; set; } = string.Empty;
    public string? ReportingPersonEmail { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime AssignedAt { get; set; }
}

public record CreateEmployeeRequest(
    string Name,
    string Email,
    string? Phone,
    string EmployeeCode,
    DateTime JoinDate,
    string? UserId,
    int? CompanyId,
    int? DepartmentId,
    int? TeamId,
    List<int>? ReportingPersonIds
);

public record UpdateEmployeeRequest(
    string Name,
    string Email,
    string? Phone,
    string EmployeeCode,
    DateTime JoinDate,
    bool? IsActive,
    string? UserId,
    int? CompanyId,
    int? DepartmentId,
    int? TeamId,
    List<int>? ReportingPersonIds
);

public record AddReportingPersonRequest(int ReportingPersonId, bool IsPrimary = false);
