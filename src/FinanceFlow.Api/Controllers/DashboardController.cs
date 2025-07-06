using FinanceFlow.Api.Data;
using FinanceFlow.Api.Models.Financial;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FinanceFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    [HttpGet("overview")]
    public async Task<ActionResult<DashboardOverviewDto>> GetDashboardOverview()
    {
        try
        {
            var userId = GetUserId();
            var currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var nextMonth = currentMonth.AddMonths(1);

            // Get current month transactions
            var currentMonthTransactions = await _context.Transactions
                .Where(t => t.UserId == userId && t.Date >= currentMonth && t.Date < nextMonth)
                .ToListAsync();

            // Get all accounts
            var accounts = await _context.Accounts
                .Where(a => a.UserId == userId && a.IsActive)
                .ToListAsync();

            // Get active budgets
            var activeBudgets = await _context.Budgets
                .Where(b => b.UserId == userId && b.IsActive && 
                           b.StartDate <= DateTime.Now && b.EndDate >= DateTime.Now)
                .ToListAsync();

            // Get active goals
            var activeGoals = await _context.Goals
                .Where(g => g.UserId == userId && g.Status == GoalStatus.Active)
                .ToListAsync();

            // Calculate metrics
            var totalBalance = accounts.Sum(a => a.Balance);
            var monthlyIncome = currentMonthTransactions
                .Where(t => t.Type == TransactionType.Income)
                .Sum(t => t.Amount);
            var monthlyExpenses = currentMonthTransactions
                .Where(t => t.Type == TransactionType.Expense)
                .Sum(t => t.Amount);

            // Budget utilization
            var totalBudgetAmount = activeBudgets.Sum(b => b.Amount);
            var budgetUtilization = totalBudgetAmount > 0 ? (monthlyExpenses / totalBudgetAmount) * 100 : 0;

            // Recent transactions
            var recentTransactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .Include(t => t.Account)
                .Include(t => t.Category)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .Take(10)
                .Select(t => new RecentTransactionDto
                {
                    Id = t.Id,
                    Description = t.Description,
                    Amount = t.Amount,
                    Type = t.Type,
                    Date = t.Date,
                    AccountName = t.Account.Name,
                    CategoryName = t.Category != null ? t.Category.Name : null,
                    CategoryColor = t.Category != null ? t.Category.Color : null,
                    CategoryIcon = t.Category != null ? t.Category.Icon : null
                })
                .ToListAsync();

            var overview = new DashboardOverviewDto
            {
                TotalBalance = totalBalance,
                MonthlyIncome = monthlyIncome,
                MonthlyExpenses = monthlyExpenses,
                NetIncome = monthlyIncome - monthlyExpenses,
                BudgetUtilization = budgetUtilization,
                TotalAccounts = accounts.Count,
                ActiveBudgets = activeBudgets.Count,
                ActiveGoals = activeGoals.Count,
                RecentTransactions = recentTransactions,
                AccountBalances = accounts.Select(a => new AccountBalanceDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Type = a.Type,
                    Balance = a.Balance,
                    Currency = a.Currency
                }).ToList()
            };

            return Ok(overview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard overview");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("spending-by-category")]
    public async Task<ActionResult<List<CategorySpendingDto>>> GetSpendingByCategory(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var userId = GetUserId();

            // Default to current month if no dates specified
            if (!fromDate.HasValue)
                fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            if (!toDate.HasValue)
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);

            var spendingData = await _context.Transactions
                .Where(t => t.UserId == userId && 
                           t.Type == TransactionType.Expense &&
                           t.Date >= fromDate && t.Date <= toDate &&
                           t.CategoryId.HasValue)
                .Include(t => t.Category)
                .GroupBy(t => new { t.CategoryId, t.Category!.Name, t.Category.Color, t.Category.Icon })
                .Select(g => new CategorySpendingDto
                {
                    CategoryId = g.Key.CategoryId!.Value,
                    CategoryName = g.Key.Name,
                    CategoryColor = g.Key.Color,
                    CategoryIcon = g.Key.Icon,
                    Amount = g.Sum(t => t.Amount),
                    TransactionCount = g.Count()
                })
                .OrderByDescending(c => c.Amount)
                .ToListAsync();

            // Calculate percentages
            var totalAmount = spendingData.Sum(c => c.Amount);
            foreach (var item in spendingData)
            {
                item.Percentage = totalAmount > 0 ? (item.Amount / totalAmount) * 100 : 0;
            }

            return Ok(spendingData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving spending by category");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("income-vs-expenses")]
    public async Task<ActionResult<List<IncomeExpenseDto>>> GetIncomeVsExpenses(
        [FromQuery] int months = 6)
    {
        try
        {
            var userId = GetUserId();
            var data = new List<IncomeExpenseDto>();
            
            for (int i = months - 1; i >= 0; i--)
            {
                var date = DateTime.Now.AddMonths(-i);
                var startDate = new DateTime(date.Year, date.Month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var transactions = await _context.Transactions
                    .Where(t => t.UserId == userId && t.Date >= startDate && t.Date <= endDate)
                    .ToListAsync();

                var income = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
                var expenses = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

                data.Add(new IncomeExpenseDto
                {
                    Month = startDate.ToString("MMM yyyy"),
                    Income = income,
                    Expenses = expenses,
                    Net = income - expenses
                });
            }

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving income vs expenses data");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("budget-performance")]
    public async Task<ActionResult<List<BudgetPerformanceDto>>> GetBudgetPerformance()
    {
        try
        {
            var userId = GetUserId();
            var activeBudgets = await _context.Budgets
                .Where(b => b.UserId == userId && b.IsActive)
                .Include(b => b.BudgetCategories)
                    .ThenInclude(bc => bc.Category)
                .ToListAsync();

            var performance = new List<BudgetPerformanceDto>();

            foreach (var budget in activeBudgets)
            {
                var spentAmount = await _context.Transactions
                    .Where(t => t.UserId == userId && 
                               t.Type == TransactionType.Expense &&
                               t.Date >= budget.StartDate && 
                               t.Date <= budget.EndDate)
                    .SumAsync(t => t.Amount);

                performance.Add(new BudgetPerformanceDto
                {
                    BudgetId = budget.Id,
                    BudgetName = budget.Name,
                    BudgetAmount = budget.Amount,
                    SpentAmount = spentAmount,
                    RemainingAmount = budget.Amount - spentAmount,
                    PercentageUsed = budget.Amount > 0 ? (spentAmount / budget.Amount) * 100 : 0,
                    IsOverBudget = spentAmount > budget.Amount,
                    DaysRemaining = (budget.EndDate - DateTime.Now).Days
                });
            }

            return Ok(performance.OrderByDescending(p => p.PercentageUsed));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving budget performance");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

// DTOs for Dashboard
public class DashboardOverviewDto
{
    public decimal TotalBalance { get; set; }
    public decimal MonthlyIncome { get; set; }
    public decimal MonthlyExpenses { get; set; }
    public decimal NetIncome { get; set; }
    public decimal BudgetUtilization { get; set; }
    public int TotalAccounts { get; set; }
    public int ActiveBudgets { get; set; }
    public int ActiveGoals { get; set; }
    public List<RecentTransactionDto> RecentTransactions { get; set; } = new();
    public List<AccountBalanceDto> AccountBalances { get; set; } = new();
}

public class RecentTransactionDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime Date { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public string? CategoryColor { get; set; }
    public string? CategoryIcon { get; set; }
}

public class AccountBalanceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public class CategorySpendingDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryColor { get; set; }
    public string? CategoryIcon { get; set; }
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public int TransactionCount { get; set; }
}

public class IncomeExpenseDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal Net { get; set; }
}

public class BudgetPerformanceDto
{
    public int BudgetId { get; set; }
    public string BudgetName { get; set; } = string.Empty;
    public decimal BudgetAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal PercentageUsed { get; set; }
    public bool IsOverBudget { get; set; }
    public int DaysRemaining { get; set; }
}