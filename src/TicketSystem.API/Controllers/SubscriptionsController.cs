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
public class SubscriptionsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(IApplicationDbContext context, ILogger<SubscriptionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedList<SubscriptionDto>>> GetSubscriptions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int? companyId = null,
        [FromQuery] int? productId = null)
    {
        var query = _context.CompanyProducts
            .Include(cp => cp.Company)
            .Include(cp => cp.Product)
            .Include(cp => cp.Plans)
                .ThenInclude(p => p.ProductPlan)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(cp => cp.Company.Name.Contains(search) || cp.Product.Name.Contains(search));

        if (isActive.HasValue)
            query = query.Where(cp => cp.IsActive == isActive.Value);

        if (companyId.HasValue)
            query = query.Where(cp => cp.CompanyId == companyId.Value);

        if (productId.HasValue)
            query = query.Where(cp => cp.ProductId == productId.Value);

        query = query.OrderByDescending(cp => cp.StartDate);

        var result = await PaginatedList<SubscriptionDto>.CreateAsync(
            query.Select(cp => new SubscriptionDto
            {
                Id = cp.Id,
                CompanyId = cp.CompanyId,
                CompanyName = cp.Company.Name,
                ProductId = cp.ProductId,
                ProductName = cp.Product.Name,
                StartDate = cp.StartDate,
                EndDate = cp.EndDate,
                IsActive = cp.IsActive,
                Plans = cp.Plans.Select(p => new SubscriptionPlanDto
                {
                    Id = p.Id,
                    ProductPlanId = p.ProductPlanId,
                    PlanName = p.ProductPlan.Name,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    IsActive = p.IsActive
                }).ToList()
            }),
            pageNumber, pageSize);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SubscriptionDto>> GetSubscription(int id)
    {
        var subscription = await _context.CompanyProducts
            .Include(cp => cp.Company)
            .Include(cp => cp.Product)
            .Include(cp => cp.Plans)
                .ThenInclude(p => p.ProductPlan)
            .FirstOrDefaultAsync(cp => cp.Id == id);

        if (subscription is null)
            return NotFound();

        return Ok(new SubscriptionDto
        {
            Id = subscription.Id,
            CompanyId = subscription.CompanyId,
            CompanyName = subscription.Company.Name,
            ProductId = subscription.ProductId,
            ProductName = subscription.Product.Name,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            IsActive = subscription.IsActive,
            Plans = subscription.Plans.Select(p => new SubscriptionPlanDto
            {
                Id = p.Id,
                ProductPlanId = p.ProductPlanId,
                PlanName = p.ProductPlan.Name,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                IsActive = p.IsActive
            }).ToList()
        });
    }

    [HttpPost]
    public async Task<ActionResult<int>> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        // Check if company exists
        var company = await _context.Companies.FindAsync(request.CompanyId);
        if (company is null)
            return NotFound(new { Message = "Company not found" });

        // Check if product exists
        var product = await _context.Products.FindAsync(request.ProductId);
        if (product is null)
            return NotFound(new { Message = "Product not found" });

        // Check for existing subscription
        var existing = await _context.CompanyProducts
            .FirstOrDefaultAsync(cp => cp.CompanyId == request.CompanyId && cp.ProductId == request.ProductId);
        if (existing is not null)
            return BadRequest(new { Message = "Subscription already exists for this company and product" });

        var subscription = new CompanyProduct
        {
            CompanyId = request.CompanyId,
            ProductId = request.ProductId,
            StartDate = request.StartDate ?? DateTime.UtcNow,
            EndDate = request.EndDate,
            IsActive = request.IsActive ?? true
        };

        _context.CompanyProducts.Add(subscription);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Subscription created for Company {CompanyId} and Product {ProductId} with ID {Id}",
            request.CompanyId, request.ProductId, subscription.Id);

        return CreatedAtAction(nameof(GetSubscription), new { id = subscription.Id }, subscription.Id);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSubscription(int id, [FromBody] UpdateSubscriptionRequest request)
    {
        var subscription = await _context.CompanyProducts.FindAsync(id);
        if (subscription is null)
            return NotFound();

        subscription.StartDate = request.StartDate ?? subscription.StartDate;
        subscription.EndDate = request.EndDate;
        subscription.IsActive = request.IsActive ?? subscription.IsActive;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSubscription(int id)
    {
        var subscription = await _context.CompanyProducts
            .Include(cp => cp.Plans)
            .FirstOrDefaultAsync(cp => cp.Id == id);

        if (subscription is null)
            return NotFound();

        _context.CompanyProducts.Remove(subscription);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Subscription Plans
    [HttpGet("{subscriptionId}/plans")]
    public async Task<ActionResult<List<SubscriptionPlanDto>>> GetSubscriptionPlans(int subscriptionId)
    {
        var plans = await _context.CompanyProductPlans
            .Include(p => p.ProductPlan)
            .Where(p => p.CompanyProductId == subscriptionId)
            .Select(p => new SubscriptionPlanDto
            {
                Id = p.Id,
                ProductPlanId = p.ProductPlanId,
                PlanName = p.ProductPlan.Name,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                IsActive = p.IsActive
            })
            .ToListAsync();

        return Ok(plans);
    }

    [HttpPost("{subscriptionId}/plans")]
    public async Task<ActionResult<int>> AddSubscriptionPlan(int subscriptionId, [FromBody] AddSubscriptionPlanRequest request)
    {
        var subscription = await _context.CompanyProducts.FindAsync(subscriptionId);
        if (subscription is null)
            return NotFound(new { Message = "Subscription not found" });

        var productPlan = await _context.ProductPlans.FindAsync(request.ProductPlanId);
        if (productPlan is null)
            return NotFound(new { Message = "Product plan not found" });

        // Check if plan already assigned
        var existing = await _context.CompanyProductPlans
            .FirstOrDefaultAsync(p => p.CompanyProductId == subscriptionId && p.ProductPlanId == request.ProductPlanId);
        if (existing is not null)
            return BadRequest(new { Message = "Plan already assigned to this subscription" });

        var subscriptionPlan = new CompanyProductPlan
        {
            CompanyProductId = subscriptionId,
            ProductPlanId = request.ProductPlanId,
            StartDate = request.StartDate ?? DateTime.UtcNow,
            EndDate = request.EndDate,
            IsActive = request.IsActive ?? true
        };

        _context.CompanyProductPlans.Add(subscriptionPlan);
        await _context.SaveChangesAsync();

        return Ok(subscriptionPlan.Id);
    }

    [HttpPut("{subscriptionId}/plans/{planId}")]
    public async Task<IActionResult> UpdateSubscriptionPlan(int subscriptionId, int planId, [FromBody] UpdateSubscriptionPlanRequest request)
    {
        var plan = await _context.CompanyProductPlans
            .FirstOrDefaultAsync(p => p.Id == planId && p.CompanyProductId == subscriptionId);

        if (plan is null)
            return NotFound();

        plan.StartDate = request.StartDate ?? plan.StartDate;
        plan.EndDate = request.EndDate;
        plan.IsActive = request.IsActive ?? plan.IsActive;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{subscriptionId}/plans/{planId}")]
    public async Task<IActionResult> RemoveSubscriptionPlan(int subscriptionId, int planId)
    {
        var plan = await _context.CompanyProductPlans
            .FirstOrDefaultAsync(p => p.Id == planId && p.CompanyProductId == subscriptionId);

        if (plan is null)
            return NotFound();

        _context.CompanyProductPlans.Remove(plan);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

// DTOs
public class SubscriptionDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public List<SubscriptionPlanDto> Plans { get; set; } = new();
}

public class SubscriptionPlanDto
{
    public int Id { get; set; }
    public int ProductPlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
}

public record CreateSubscriptionRequest(
    int CompanyId,
    int ProductId,
    DateTime? StartDate,
    DateTime? EndDate,
    bool? IsActive);

public record UpdateSubscriptionRequest(
    DateTime? StartDate,
    DateTime? EndDate,
    bool? IsActive);

public record AddSubscriptionPlanRequest(
    int ProductPlanId,
    DateTime? StartDate,
    DateTime? EndDate,
    bool? IsActive);

public record UpdateSubscriptionPlanRequest(
    DateTime? StartDate,
    DateTime? EndDate,
    bool? IsActive);
