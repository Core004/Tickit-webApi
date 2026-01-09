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
public class ProductsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IApplicationDbContext context, ILogger<ProductsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedList<ProductDto>>> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Name.Contains(search) || (p.Description != null && p.Description.Contains(search)));

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        query = query.OrderBy(p => p.Name);

        var result = await PaginatedList<ProductDto>.CreateAsync(
            query.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
            }),
            pageNumber, pageSize);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDetailDto>> GetProduct(int id)
    {
        var product = await _context.Products
            .Include(p => p.Plans)
            .Include(p => p.Versions)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
            return NotFound();

        return Ok(new ProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            Plans = product.Plans.Select(pl => new ProductPlanDto
            {
                Id = pl.Id,
                Name = pl.Name,
                Description = pl.Description,
                Price = pl.Price,
                BillingCycle = pl.BillingCycle.ToString(),
                IsActive = pl.IsActive
            }).ToList(),
            Versions = product.Versions.OrderByDescending(v => v.ReleaseDate).Select(v => new ProductVersionDto
            {
                Id = v.Id,
                Version = v.Version,
                ReleaseNotes = v.ReleaseNotes,
                ReleaseDate = v.ReleaseDate,
                IsActive = v.IsActive
            }).ToList()
        });
    }

    [HttpPost]
    public async Task<ActionResult<int>> CreateProduct([FromBody] CreateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product {Name} created with ID {Id}", product.Name, product.Id);

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product.Id);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null)
            return NotFound();

        product.Name = request.Name;
        product.Description = request.Description;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/activate")]
    public async Task<IActionResult> ActivateProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null)
            return NotFound();

        product.IsActive = true;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> DeactivateProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null)
            return NotFound();

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products
            .Include(p => p.Plans)
            .Include(p => p.Versions)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
            return NotFound();

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Product Plans
    [HttpGet("{productId}/plans")]
    public async Task<ActionResult<List<ProductPlanDto>>> GetProductPlans(int productId)
    {
        var plans = await _context.ProductPlans
            .Where(p => p.ProductId == productId)
            .OrderBy(p => p.Price)
            .Select(p => new ProductPlanDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                BillingCycle = p.BillingCycle.ToString(),
                IsActive = p.IsActive
            })
            .ToListAsync();

        return Ok(plans);
    }

    [HttpPost("{productId}/plans")]
    public async Task<ActionResult<int>> CreateProductPlan(int productId, [FromBody] CreateProductPlanRequest request)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product is null)
            return NotFound(new { Message = "Product not found" });

        var plan = new ProductPlan
        {
            ProductId = productId,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            BillingCycle = request.BillingCycle,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductPlans.Add(plan);
        await _context.SaveChangesAsync();

        return Ok(plan.Id);
    }

    [HttpPut("plans/{planId}")]
    public async Task<IActionResult> UpdateProductPlan(int planId, [FromBody] UpdateProductPlanRequest request)
    {
        var plan = await _context.ProductPlans.FindAsync(planId);
        if (plan is null)
            return NotFound();

        plan.Name = request.Name;
        plan.Description = request.Description;
        plan.Price = request.Price;
        plan.BillingCycle = request.BillingCycle;
        plan.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("plans/{planId}")]
    public async Task<IActionResult> DeleteProductPlan(int planId)
    {
        var plan = await _context.ProductPlans.FindAsync(planId);
        if (plan is null)
            return NotFound();

        _context.ProductPlans.Remove(plan);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Product Versions
    [HttpGet("{productId}/versions")]
    public async Task<ActionResult<List<ProductVersionDto>>> GetProductVersions(int productId)
    {
        var versions = await _context.ProductVersions
            .Where(v => v.ProductId == productId)
            .OrderByDescending(v => v.ReleaseDate)
            .Select(v => new ProductVersionDto
            {
                Id = v.Id,
                Version = v.Version,
                ReleaseNotes = v.ReleaseNotes,
                ReleaseDate = v.ReleaseDate,
                IsActive = v.IsActive
            })
            .ToListAsync();

        return Ok(versions);
    }

    [HttpPost("{productId}/versions")]
    public async Task<ActionResult<int>> CreateProductVersion(int productId, [FromBody] CreateProductVersionRequest request)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product is null)
            return NotFound(new { Message = "Product not found" });

        var version = new ProductVersion
        {
            ProductId = productId,
            Version = request.Version,
            ReleaseNotes = request.ReleaseNotes,
            ReleaseDate = request.ReleaseDate ?? DateTime.UtcNow,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductVersions.Add(version);
        await _context.SaveChangesAsync();

        return Ok(version.Id);
    }

    [HttpDelete("versions/{versionId}")]
    public async Task<IActionResult> DeleteProductVersion(int versionId)
    {
        var version = await _context.ProductVersions.FindAsync(versionId);
        if (version is null)
            return NotFound();

        _context.ProductVersions.Remove(version);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

// DTOs
public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProductDetailDto : ProductDto
{
    public DateTime? UpdatedAt { get; set; }
    public List<ProductPlanDto> Plans { get; set; } = new();
    public List<ProductVersionDto> Versions { get; set; } = new();
}

public class ProductPlanDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string BillingCycle { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class ProductVersionDto
{
    public int Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string? ReleaseNotes { get; set; }
    public DateTime ReleaseDate { get; set; }
    public bool IsActive { get; set; }
}

public record CreateProductRequest(string Name, string? Description);
public record UpdateProductRequest(string Name, string? Description);
public record CreateProductPlanRequest(
    string Name,
    string? Description,
    decimal Price,
    Domain.Enums.BillingCycle BillingCycle);
public record UpdateProductPlanRequest(
    string Name,
    string? Description,
    decimal Price,
    Domain.Enums.BillingCycle BillingCycle);
public record CreateProductVersionRequest(
    string Version,
    string? ReleaseNotes,
    DateTime? ReleaseDate,
    bool IsActive = true);
