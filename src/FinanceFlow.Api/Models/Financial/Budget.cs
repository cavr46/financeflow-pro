using System.ComponentModel.DataAnnotations;

namespace FinanceFlow.Api.Models.Financial;

public class Budget
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Required]
    public decimal Amount { get; set; }
    
    public BudgetPeriod Period { get; set; }
    
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public bool AlertEnabled { get; set; } = true;
    
    public decimal AlertThreshold { get; set; } = 0.8m; // 80% threshold
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public ICollection<BudgetCategory> BudgetCategories { get; set; } = new List<BudgetCategory>();
}

public class BudgetCategory
{
    public int Id { get; set; }
    
    [Required]
    public int BudgetId { get; set; }
    
    [Required]
    public int CategoryId { get; set; }
    
    [Required]
    public decimal AllocatedAmount { get; set; }
    
    public decimal SpentAmount { get; set; } = 0;
    
    // Navigation properties
    public Budget Budget { get; set; } = null!;
    public Category Category { get; set; } = null!;
}

public enum BudgetPeriod
{
    Weekly = 0,
    Monthly = 1,
    Quarterly = 2,
    Yearly = 3,
    Custom = 4
}