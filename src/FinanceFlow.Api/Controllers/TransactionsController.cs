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
public class TransactionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(ApplicationDbContext context, ILogger<TransactionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    [HttpGet]
    public async Task<ActionResult<List<TransactionDto>>> GetTransactions(
        [FromQuery] int? accountId = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] TransactionType? type = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var userId = GetUserId();
            var query = _context.Transactions
                .Where(t => t.UserId == userId)
                .Include(t => t.Account)
                .Include(t => t.ToAccount)
                .Include(t => t.Category)
                .Include(t => t.TransactionTags)
                    .ThenInclude(tt => tt.Tag)
                .AsQueryable();

            if (accountId.HasValue)
                query = query.Where(t => t.AccountId == accountId.Value);

            if (categoryId.HasValue)
                query = query.Where(t => t.CategoryId == categoryId.Value);

            if (type.HasValue)
                query = query.Where(t => t.Type == type.Value);

            if (fromDate.HasValue)
                query = query.Where(t => t.Date >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(t => t.Date <= toDate.Value);

            var transactions = await query
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TransactionDto
                {
                    Id = t.Id,
                    Description = t.Description,
                    Amount = t.Amount,
                    Type = t.Type,
                    Date = t.Date,
                    Notes = t.Notes,
                    Reference = t.Reference,
                    IsRecurring = t.IsRecurring,
                    RecurrenceType = t.RecurrenceType,
                    AccountId = t.AccountId,
                    AccountName = t.Account.Name,
                    CategoryId = t.CategoryId,
                    CategoryName = t.Category != null ? t.Category.Name : null,
                    CategoryColor = t.Category != null ? t.Category.Color : null,
                    CategoryIcon = t.Category != null ? t.Category.Icon : null,
                    ToAccountId = t.ToAccountId,
                    ToAccountName = t.ToAccount != null ? t.ToAccount.Name : null,
                    Tags = t.TransactionTags.Select(tt => new TagDto
                    {
                        Id = tt.Tag.Id,
                        Name = tt.Tag.Name,
                        Color = tt.Tag.Color,
                        CreatedAt = tt.Tag.CreatedAt
                    }).ToList(),
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                })
                .ToListAsync();

            return Ok(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transactions");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionDto>> GetTransaction(int id)
    {
        try
        {
            var userId = GetUserId();
            var transaction = await _context.Transactions
                .Where(t => t.Id == id && t.UserId == userId)
                .Include(t => t.Account)
                .Include(t => t.ToAccount)
                .Include(t => t.Category)
                .Include(t => t.TransactionTags)
                    .ThenInclude(tt => tt.Tag)
                .Select(t => new TransactionDto
                {
                    Id = t.Id,
                    Description = t.Description,
                    Amount = t.Amount,
                    Type = t.Type,
                    Date = t.Date,
                    Notes = t.Notes,
                    Reference = t.Reference,
                    IsRecurring = t.IsRecurring,
                    RecurrenceType = t.RecurrenceType,
                    AccountId = t.AccountId,
                    AccountName = t.Account.Name,
                    CategoryId = t.CategoryId,
                    CategoryName = t.Category != null ? t.Category.Name : null,
                    CategoryColor = t.Category != null ? t.Category.Color : null,
                    CategoryIcon = t.Category != null ? t.Category.Icon : null,
                    ToAccountId = t.ToAccountId,
                    ToAccountName = t.ToAccount != null ? t.ToAccount.Name : null,
                    Tags = t.TransactionTags.Select(tt => new TagDto
                    {
                        Id = tt.Tag.Id,
                        Name = tt.Tag.Name,
                        Color = tt.Tag.Color,
                        CreatedAt = tt.Tag.CreatedAt
                    }).ToList(),
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (transaction == null)
                return NotFound(new { message = "Transaction not found" });

            return Ok(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transaction {TransactionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> CreateTransaction([FromBody] CreateTransactionDto dto)
    {
        try
        {
            var userId = GetUserId();

            // Verify account belongs to user
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == dto.AccountId && a.UserId == userId);
            if (account == null)
                return BadRequest(new { message = "Invalid account" });

            // Verify toAccount if specified
            if (dto.ToAccountId.HasValue)
            {
                var toAccount = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.Id == dto.ToAccountId.Value && a.UserId == userId);
                if (toAccount == null)
                    return BadRequest(new { message = "Invalid destination account" });
            }

            // Verify category if specified
            if (dto.CategoryId.HasValue)
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == dto.CategoryId.Value && (c.UserId == userId || c.UserId == "seed"));
                if (category == null)
                    return BadRequest(new { message = "Invalid category" });
            }

            var transaction = new Transaction
            {
                Description = dto.Description,
                Amount = dto.Amount,
                Type = dto.Type,
                Date = dto.Date,
                Notes = dto.Notes,
                Reference = dto.Reference,
                IsRecurring = dto.IsRecurring,
                RecurrenceType = dto.RecurrenceType,
                AccountId = dto.AccountId,
                CategoryId = dto.CategoryId,
                ToAccountId = dto.ToAccountId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);

            // Update account balances
            if (dto.Type == TransactionType.Income)
            {
                account.Balance += dto.Amount;
            }
            else if (dto.Type == TransactionType.Expense)
            {
                account.Balance -= dto.Amount;
            }
            else if (dto.Type == TransactionType.Transfer && dto.ToAccountId.HasValue)
            {
                account.Balance -= dto.Amount;
                var toAccount = await _context.Accounts.FindAsync(dto.ToAccountId.Value);
                if (toAccount != null)
                    toAccount.Balance += dto.Amount;
            }

            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Add tags if specified
            if (dto.TagIds?.Any() == true)
            {
                var tags = await _context.Tags
                    .Where(t => dto.TagIds.Contains(t.Id) && t.UserId == userId)
                    .ToListAsync();

                foreach (var tag in tags)
                {
                    _context.TransactionTags.Add(new TransactionTag
                    {
                        TransactionId = transaction.Id,
                        TagId = tag.Id
                    });
                }

                await _context.SaveChangesAsync();
            }

            // Return the created transaction
            var createdTransaction = await GetTransaction(transaction.Id);
            _logger.LogInformation("Transaction created: {TransactionId} for user {UserId}", transaction.Id, userId);
            
            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, createdTransaction.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transaction");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("summary")]
    public async Task<ActionResult<TransactionSummaryDto>> GetTransactionSummary(
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

            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId && t.Date >= fromDate && t.Date <= toDate)
                .ToListAsync();

            var summary = new TransactionSummaryDto
            {
                TotalIncome = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                TotalExpenses = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount),
                TransactionCount = transactions.Count,
                FromDate = fromDate.Value,
                ToDate = toDate.Value
            };

            summary.NetAmount = summary.TotalIncome - summary.TotalExpenses;

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transaction summary");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}