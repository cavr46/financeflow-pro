using FinanceFlow.Api.Data;
using FinanceFlow.Api.Models.DTOs;
using FinanceFlow.Api.Models.Financial;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FinanceFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BudgetsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BudgetsController> _logger;

    public BudgetsController(ApplicationDbContext context, ILogger<BudgetsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    [HttpGet]
    public async Task<ActionResult<List<BudgetDto>>> GetBudgets([FromQuery] bool activeOnly = false)
    {
        try
        {
            var userId = GetUserId();
            var query = _context.Budgets
                .Where(b => b.UserId == userId)
                .Include(b => b.BudgetCategories)
                    .ThenInclude(bc => bc.Category)
                .AsQueryable();

            if (activeOnly)
                query = query.Where(b => b.IsActive);

            var budgets = await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var budgetDtos = new List<BudgetDto>();

            foreach (var budget in budgets)
            {
                // Calculate spent amounts for this budget period
                var spentAmounts = await CalculateSpentAmounts(budget, userId);

                var budgetDto = new BudgetDto
                {
                    Id = budget.Id,
                    Name = budget.Name,
                    Description = budget.Description,
                    Amount = budget.Amount,
                    Period = budget.Period,
                    PeriodName = budget.Period.ToString(),
                    StartDate = budget.StartDate,
                    EndDate = budget.EndDate,
                    IsActive = budget.IsActive,
                    AlertEnabled = budget.AlertEnabled,
                    AlertThreshold = budget.AlertThreshold,
                    SpentAmount = spentAmounts.TotalSpent,
                    RemainingAmount = budget.Amount - spentAmounts.TotalSpent,
                    PercentageUsed = budget.Amount > 0 ? (spentAmounts.TotalSpent / budget.Amount) * 100 : 0,
                    IsOverBudget = spentAmounts.TotalSpent > budget.Amount,
                    IsAlertTriggered = spentAmounts.TotalSpent >= (budget.Amount * budget.AlertThreshold),
                    CreatedAt = budget.CreatedAt,
                    UpdatedAt = budget.UpdatedAt,
                    Categories = budget.BudgetCategories.Select(bc => new BudgetCategoryDto
                    {
                        Id = bc.Id,
                        CategoryId = bc.CategoryId,
                        CategoryName = bc.Category.Name,
                        CategoryColor = bc.Category.Color,
                        CategoryIcon = bc.Category.Icon,
                        AllocatedAmount = bc.AllocatedAmount,
                        SpentAmount = spentAmounts.CategorySpent.GetValueOrDefault(bc.CategoryId, 0),
                        RemainingAmount = bc.AllocatedAmount - spentAmounts.CategorySpent.GetValueOrDefault(bc.CategoryId, 0),
                        PercentageUsed = bc.AllocatedAmount > 0 ? (spentAmounts.CategorySpent.GetValueOrDefault(bc.CategoryId, 0) / bc.AllocatedAmount) * 100 : 0,
                        IsOverBudget = spentAmounts.CategorySpent.GetValueOrDefault(bc.CategoryId, 0) > bc.AllocatedAmount
                    }).ToList()
                };

                budgetDtos.Add(budgetDto);
            }

            return Ok(budgetDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving budgets");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BudgetDto>> GetBudget(int id)
    {
        try
        {
            var userId = GetUserId();
            var budget = await _context.Budgets
                .Where(b => b.Id == id && b.UserId == userId)
                .Include(b => b.BudgetCategories)
                    .ThenInclude(bc => bc.Category)
                .FirstOrDefaultAsync();

            if (budget == null)
                return NotFound(new { message = "Budget not found" });

            // Calculate spent amounts for this budget period
            var spentAmounts = await CalculateSpentAmounts(budget, userId);

            var budgetDto = new BudgetDto
            {
                Id = budget.Id,
                Name = budget.Name,
                Description = budget.Description,
                Amount = budget.Amount,
                Period = budget.Period,
                PeriodName = budget.Period.ToString(),
                StartDate = budget.StartDate,
                EndDate = budget.EndDate,
                IsActive = budget.IsActive,
                AlertEnabled = budget.AlertEnabled,
                AlertThreshold = budget.AlertThreshold,
                SpentAmount = spentAmounts.TotalSpent,
                RemainingAmount = budget.Amount - spentAmounts.TotalSpent,
                PercentageUsed = budget.Amount > 0 ? (spentAmounts.TotalSpent / budget.Amount) * 100 : 0,
                IsOverBudget = spentAmounts.TotalSpent > budget.Amount,
                IsAlertTriggered = spentAmounts.TotalSpent >= (budget.Amount * budget.AlertThreshold),
                CreatedAt = budget.CreatedAt,
                UpdatedAt = budget.UpdatedAt,
                Categories = budget.BudgetCategories.Select(bc => new BudgetCategoryDto
                {
                    Id = bc.Id,
                    CategoryId = bc.CategoryId,
                    CategoryName = bc.Category.Name,
                    CategoryColor = bc.Category.Color,
                    CategoryIcon = bc.Category.Icon,
                    AllocatedAmount = bc.AllocatedAmount,
                    SpentAmount = spentAmounts.CategorySpent.GetValueOrDefault(bc.CategoryId, 0),
                    RemainingAmount = bc.AllocatedAmount - spentAmounts.CategorySpent.GetValueOrDefault(bc.CategoryId, 0),
                    PercentageUsed = bc.AllocatedAmount > 0 ? (spentAmounts.CategorySpent.GetValueOrDefault(bc.CategoryId, 0) / bc.AllocatedAmount) * 100 : 0,
                    IsOverBudget = spentAmounts.CategorySpent.GetValueOrDefault(bc.CategoryId, 0) > bc.AllocatedAmount
                }).ToList()
            };

            return Ok(budgetDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving budget {BudgetId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<BudgetDto>> CreateBudget([FromBody] CreateBudgetDto dto)
    {
        try
        {
            var userId = GetUserId();

            // Validate categories if provided
            if (dto.Categories?.Any() == true)
            {
                var categoryIds = dto.Categories.Select(c => c.CategoryId).ToList();
                var validCategories = await _context.Categories
                    .Where(c => categoryIds.Contains(c.Id) && (c.UserId == userId || c.UserId == "seed"))
                    .CountAsync();

                if (validCategories != categoryIds.Count)
                    return BadRequest(new { message = "One or more invalid categories" });
            }

            var budget = new Budget
            {
                Name = dto.Name,
                Description = dto.Description,
                Amount = dto.Amount,
                Period = dto.Period,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                AlertEnabled = dto.AlertEnabled,
                AlertThreshold = dto.AlertThreshold,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Budgets.Add(budget);
            await _context.SaveChangesAsync();

            // Add budget categories if provided
            if (dto.Categories?.Any() == true)
            {
                foreach (var categoryDto in dto.Categories)
                {
                    var budgetCategory = new BudgetCategory
                    {
                        BudgetId = budget.Id,
                        CategoryId = categoryDto.CategoryId,
                        AllocatedAmount = categoryDto.AllocatedAmount
                    };
                    _context.BudgetCategories.Add(budgetCategory);
                }
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Budget created: {BudgetId} for user {UserId}", budget.Id, userId);
            
            // Return the created budget
            var result = await GetBudget(budget.Id);
            return CreatedAtAction(nameof(GetBudget), new { id = budget.Id }, result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating budget");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("summary")]
    public async Task<ActionResult<BudgetSummaryDto>> GetBudgetSummary()
    {
        try
        {
            var userId = GetUserId();
            var budgets = await _context.Budgets
                .Where(b => b.UserId == userId)
                .Include(b => b.BudgetCategories)
                .ToListAsync();

            var totalSpent = 0m;
            var overBudgetCount = 0;
            var alertTriggeredCount = 0;

            foreach (var budget in budgets)
            {
                var spentAmounts = await CalculateSpentAmounts(budget, userId);
                totalSpent += spentAmounts.TotalSpent;

                if (spentAmounts.TotalSpent > budget.Amount)
                    overBudgetCount++;

                if (spentAmounts.TotalSpent >= (budget.Amount * budget.AlertThreshold))
                    alertTriggeredCount++;
            }

            var summary = new BudgetSummaryDto
            {
                TotalBudgetAmount = budgets.Sum(b => b.Amount),
                TotalSpentAmount = totalSpent,
                TotalRemainingAmount = budgets.Sum(b => b.Amount) - totalSpent,
                OverallPercentageUsed = budgets.Sum(b => b.Amount) > 0 ? (totalSpent / budgets.Sum(b => b.Amount)) * 100 : 0,
                TotalBudgets = budgets.Count,
                ActiveBudgets = budgets.Count(b => b.IsActive),
                OverBudgetCount = overBudgetCount,
                AlertTriggeredCount = alertTriggeredCount
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving budget summary");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    private async Task<(decimal TotalSpent, Dictionary<int, decimal> CategorySpent)> CalculateSpentAmounts(Budget budget, string userId)
    {
        var transactions = await _context.Transactions
            .Where(t => t.UserId == userId && 
                       t.Type == TransactionType.Expense &&
                       t.Date >= budget.StartDate && 
                       t.Date <= budget.EndDate)
            .ToListAsync();

        var totalSpent = transactions.Sum(t => t.Amount);
        var categorySpent = transactions
            .Where(t => t.CategoryId.HasValue)
            .GroupBy(t => t.CategoryId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        return (totalSpent, categorySpent);
    }
}