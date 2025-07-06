using System.ComponentModel.DataAnnotations;
using FinanceFlow.Api.Models.Financial;

namespace FinanceFlow.Api.Models.DTOs;

public class CreateTransactionDto
{
    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
    
    [Required]
    public TransactionType Type { get; set; }
    
    [Required]
    public DateTime Date { get; set; }
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    [MaxLength(200)]
    public string? Reference { get; set; }
    
    public bool IsRecurring { get; set; } = false;
    
    public RecurrenceType? RecurrenceType { get; set; }
    
    [Required]
    public int AccountId { get; set; }
    
    public int? CategoryId { get; set; }
    
    public int? ToAccountId { get; set; }
    
    public List<int>? TagIds { get; set; }
}

public class UpdateTransactionDto
{
    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
    
    [Required]
    public TransactionType Type { get; set; }
    
    [Required]
    public DateTime Date { get; set; }
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    [MaxLength(200)]
    public string? Reference { get; set; }
    
    public bool IsRecurring { get; set; } = false;
    
    public RecurrenceType? RecurrenceType { get; set; }
    
    [Required]
    public int AccountId { get; set; }
    
    public int? CategoryId { get; set; }
    
    public int? ToAccountId { get; set; }
    
    public List<int>? TagIds { get; set; }
}

public class TransactionDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public string? Reference { get; set; }
    public bool IsRecurring { get; set; }
    public RecurrenceType? RecurrenceType { get; set; }
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? CategoryColor { get; set; }
    public string? CategoryIcon { get; set; }
    public int? ToAccountId { get; set; }
    public string? ToAccountName { get; set; }
    public List<TagDto> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TransactionSummaryDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetAmount { get; set; }
    public int TransactionCount { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
}