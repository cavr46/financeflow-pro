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
public class AccountsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(ApplicationDbContext context, ILogger<AccountsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    [HttpGet]
    public async Task<ActionResult<List<AccountDto>>> GetAccounts()
    {
        try
        {
            var userId = GetUserId();
            var accounts = await _context.Accounts
                .Where(a => a.UserId == userId)
                .Include(a => a.Transactions)
                .OrderBy(a => a.Name)
                .Select(a => new AccountDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Description = a.Description,
                    Type = a.Type,
                    TypeName = a.Type.ToString(),
                    Balance = a.Balance,
                    Currency = a.Currency,
                    IsActive = a.IsActive,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt,
                    TransactionCount = a.Transactions.Count
                })
                .ToListAsync();

            return Ok(accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving accounts");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AccountDto>> GetAccount(int id)
    {
        try
        {
            var userId = GetUserId();
            var account = await _context.Accounts
                .Where(a => a.Id == id && a.UserId == userId)
                .Include(a => a.Transactions)
                .Select(a => new AccountDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Description = a.Description,
                    Type = a.Type,
                    TypeName = a.Type.ToString(),
                    Balance = a.Balance,
                    Currency = a.Currency,
                    IsActive = a.IsActive,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt,
                    TransactionCount = a.Transactions.Count
                })
                .FirstOrDefaultAsync();

            if (account == null)
                return NotFound(new { message = "Account not found" });

            return Ok(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account {AccountId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<AccountDto>> CreateAccount([FromBody] CreateAccountDto dto)
    {
        try
        {
            var userId = GetUserId();
            var account = new Account
            {
                Name = dto.Name,
                Description = dto.Description,
                Type = dto.Type,
                Balance = dto.Balance,
                Currency = dto.Currency,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            var accountDto = new AccountDto
            {
                Id = account.Id,
                Name = account.Name,
                Description = account.Description,
                Type = account.Type,
                TypeName = account.Type.ToString(),
                Balance = account.Balance,
                Currency = account.Currency,
                IsActive = account.IsActive,
                CreatedAt = account.CreatedAt,
                UpdatedAt = account.UpdatedAt,
                TransactionCount = 0
            };

            _logger.LogInformation("Account created: {AccountId} for user {UserId}", account.Id, userId);
            return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, accountDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AccountDto>> UpdateAccount(int id, [FromBody] UpdateAccountDto dto)
    {
        try
        {
            var userId = GetUserId();
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (account == null)
                return NotFound(new { message = "Account not found" });

            account.Name = dto.Name;
            account.Description = dto.Description;
            account.Type = dto.Type;
            account.Balance = dto.Balance;
            account.Currency = dto.Currency;
            account.IsActive = dto.IsActive;
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var accountDto = new AccountDto
            {
                Id = account.Id,
                Name = account.Name,
                Description = account.Description,
                Type = account.Type,
                TypeName = account.Type.ToString(),
                Balance = account.Balance,
                Currency = account.Currency,
                IsActive = account.IsActive,
                CreatedAt = account.CreatedAt,
                UpdatedAt = account.UpdatedAt,
                TransactionCount = await _context.Transactions.CountAsync(t => t.AccountId == account.Id)
            };

            _logger.LogInformation("Account updated: {AccountId}", id);
            return Ok(accountDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account {AccountId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAccount(int id)
    {
        try
        {
            var userId = GetUserId();
            var account = await _context.Accounts
                .Include(a => a.Transactions)
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (account == null)
                return NotFound(new { message = "Account not found" });

            if (account.Transactions.Any())
                return BadRequest(new { message = "Cannot delete account with existing transactions" });

            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Account deleted: {AccountId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account {AccountId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AccountSummaryDto>> GetAccountSummary()
    {
        try
        {
            var userId = GetUserId();
            var accounts = await _context.Accounts
                .Where(a => a.UserId == userId)
                .ToListAsync();

            var summary = new AccountSummaryDto
            {
                TotalBalance = accounts.Sum(a => a.Balance),
                TotalAccounts = accounts.Count,
                ActiveAccounts = accounts.Count(a => a.IsActive),
                BalanceByType = accounts
                    .GroupBy(a => a.Type)
                    .Select(g => new AccountTypeBalance
                    {
                        Type = g.Key,
                        TypeName = g.Key.ToString(),
                        Balance = g.Sum(a => a.Balance),
                        AccountCount = g.Count()
                    })
                    .ToList()
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account summary");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}