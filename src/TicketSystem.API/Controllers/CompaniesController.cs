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
public class CompaniesController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CompaniesController> _logger;

    public CompaniesController(IApplicationDbContext context, ILogger<CompaniesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedList<CompanyDto>>> GetCompanies(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        var query = _context.Companies.AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(c => c.Name.Contains(search) || (c.Email != null && c.Email.Contains(search)));

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        query = query.OrderBy(c => c.Name);

        var result = await PaginatedList<CompanyDto>.CreateAsync(
            query.Select(c => new CompanyDto
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email,
                MobileNo = c.MobileNo,
                PhoneNo = c.PhoneNo,
                Website = c.Website,
                City = c.City,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            }),
            pageNumber, pageSize);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CompanyDetailDto>> GetCompany(int id)
    {
        var company = await _context.Companies
            .Include(c => c.DepartmentCompanies)
            .Include(c => c.Users)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (company is null)
            return NotFound();

        return Ok(new CompanyDetailDto
        {
            Id = company.Id,
            Name = company.Name,
            Email = company.Email,
            MobileNo = company.MobileNo,
            PhoneNo = company.PhoneNo,
            Website = company.Website,
            Address = company.Address,
            AddressLine1 = company.AddressLine1,
            AddressLine2 = company.AddressLine2,
            City = company.City,
            Area = company.Area,
            PinCode = company.PinCode,
            Description = company.Description,
            IsActive = company.IsActive,
            CreatedAt = company.CreatedAt,
            UpdatedAt = company.UpdatedAt,
            DepartmentCount = company.DepartmentCompanies.Count,
            UserCount = company.Users.Count
        });
    }

    [HttpPost]
    public async Task<ActionResult<int>> CreateCompany([FromBody] CreateCompanyRequest request)
    {
        var company = new Company
        {
            Name = request.Name,
            Email = request.Email,
            MobileNo = request.MobileNo,
            PhoneNo = request.PhoneNo,
            Website = request.Website,
            Address = request.Address,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            Area = request.Area,
            PinCode = request.PinCode,
            Description = request.Description,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        // Create product subscriptions if ProductIds provided
        if (request.ProductIds?.Count > 0)
        {
            var companyProducts = request.ProductIds.Select(productId => new CompanyProduct
            {
                CompanyId = company.Id,
                ProductId = productId,
                StartDate = DateTime.UtcNow,
                IsActive = true
            }).ToList();

            _context.CompanyProducts.AddRange(companyProducts);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Company {Name} created with ID {Id}", company.Name, company.Id);

        return CreatedAtAction(nameof(GetCompany), new { id = company.Id }, company.Id);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCompany(int id, [FromBody] UpdateCompanyRequest request)
    {
        var company = await _context.Companies.FindAsync(id);
        if (company is null)
            return NotFound();

        company.Name = request.Name;
        company.Email = request.Email;
        company.MobileNo = request.MobileNo;
        company.PhoneNo = request.PhoneNo;
        company.Website = request.Website;
        company.Address = request.Address;
        company.AddressLine1 = request.AddressLine1;
        company.AddressLine2 = request.AddressLine2;
        company.City = request.City;
        company.Area = request.Area;
        company.PinCode = request.PinCode;
        company.Description = request.Description;
        company.IsActive = request.IsActive;
        company.UpdatedAt = DateTime.UtcNow;

        // Sync product subscriptions if ProductIds provided
        if (request.ProductIds is not null)
        {
            var existingProducts = await _context.CompanyProducts
                .Where(cp => cp.CompanyId == id)
                .ToListAsync();

            var existingProductIds = existingProducts.Select(cp => cp.ProductId).ToHashSet();
            var newProductIds = request.ProductIds.ToHashSet();

            // Remove products that are no longer selected
            var toRemove = existingProducts.Where(cp => !newProductIds.Contains(cp.ProductId)).ToList();
            if (toRemove.Count > 0)
            {
                _context.CompanyProducts.RemoveRange(toRemove);
            }

            // Add newly selected products
            var toAdd = newProductIds.Except(existingProductIds).Select(productId => new CompanyProduct
            {
                CompanyId = id,
                ProductId = productId,
                StartDate = DateTime.UtcNow,
                IsActive = true
            }).ToList();

            if (toAdd.Count > 0)
            {
                _context.CompanyProducts.AddRange(toAdd);
            }
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/activate")]
    public async Task<IActionResult> ActivateCompany(int id)
    {
        var company = await _context.Companies.FindAsync(id);
        if (company is null)
            return NotFound();

        company.IsActive = true;
        company.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> DeactivateCompany(int id)
    {
        var company = await _context.Companies.FindAsync(id);
        if (company is null)
            return NotFound();

        company.IsActive = false;
        company.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCompany(int id)
    {
        var company = await _context.Companies.FindAsync(id);
        if (company is null)
            return NotFound();

        _context.Companies.Remove(company);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Company {Id} deleted", id);

        return NoContent();
    }

    [HttpGet("{id}/products")]
    public async Task<ActionResult<List<int>>> GetCompanyProducts(int id)
    {
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == id);
        if (!companyExists)
            return NotFound();

        var productIds = await _context.CompanyProducts
            .Where(cp => cp.CompanyId == id && cp.IsActive)
            .Select(cp => cp.ProductId)
            .ToListAsync();

        return Ok(productIds);
    }
}

// DTOs
public class CompanyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? MobileNo { get; set; }
    public string? PhoneNo { get; set; }
    public string? Website { get; set; }
    public string? City { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CompanyDetailDto : CompanyDto
{
    public string? Address { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? Area { get; set; }
    public string? PinCode { get; set; }
    public string? Description { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int DepartmentCount { get; set; }
    public int UserCount { get; set; }
}

public record CreateCompanyRequest(
    string Name,
    string? Email,
    string? MobileNo,
    string? PhoneNo,
    string? Website,
    string? Address,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Area,
    string? PinCode,
    string? Description,
    bool IsActive = true,
    List<int>? ProductIds = null);

public record UpdateCompanyRequest(
    string Name,
    string? Email,
    string? MobileNo,
    string? PhoneNo,
    string? Website,
    string? Address,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Area,
    string? PinCode,
    string? Description,
    bool IsActive = true,
    List<int>? ProductIds = null);
