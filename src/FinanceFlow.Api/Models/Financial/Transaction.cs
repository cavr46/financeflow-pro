using System.ComponentModel.DataAnnotations;

namespace FinanceFlow.Api.Models.Financial;

public class Transaction
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public decimal Amount { get; set; }
    
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
    
    public int? ToAccountId { get; set; } // For transfers
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public Account Account { get; set; } = null!;
    public Account? ToAccount { get; set; }
    public Category? Category { get; set; }
    public ICollection<TransactionTag> TransactionTags { get; set; } = new List<TransactionTag>();
}

public enum TransactionType
{
    Income = 0,
    Expense = 1,
    Transfer = 2
}

public enum RecurrenceType
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2,
    Quarterly = 3,
    Yearly = 4
}