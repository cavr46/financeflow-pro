using System.ComponentModel.DataAnnotations;
using FinanceFlow.Api.Models.Financial;

namespace FinanceFlow.Api.Models.DTOs;

public class CreateAccountDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Required]
    public AccountType Type { get; set; }
    
    [Required]
    public decimal Balance { get; set; }
    
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";
}

public class UpdateAccountDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Required]
    public AccountType Type { get; set; }
    
    [Required]
    public decimal Balance { get; set; }
    
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";
    
    public bool IsActive { get; set; } = true;
}

public class AccountDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AccountType Type { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int TransactionCount { get; set; }
}

public class AccountSummaryDto
{
    public decimal TotalBalance { get; set; }
    public List<AccountTypeBalance> BalanceByType { get; set; } = new();
    public int TotalAccounts { get; set; }
    public int ActiveAccounts { get; set; }
}

public class AccountTypeBalance
{
    public AccountType Type { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public int AccountCount { get; set; }
}