using System.ComponentModel.DataAnnotations;
using FinanceFlow.Api.Models.Financial;

namespace FinanceFlow.Api.Models.DTOs;

public class CreateBudgetDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
    
    [Required]
    public BudgetPeriod Period { get; set; }
    
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    public bool AlertEnabled { get; set; } = true;
    
    [Range(0.1, 1.0, ErrorMessage = "Alert threshold must be between 10% and 100%")]
    public decimal AlertThreshold { get; set; } = 0.8m;
    
    public List<BudgetCategoryDto>? Categories { get; set; }
}

public class UpdateBudgetDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
    
    [Required]
    public BudgetPeriod Period { get; set; }
    
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public bool AlertEnabled { get; set; } = true;
    
    [Range(0.1, 1.0, ErrorMessage = "Alert threshold must be between 10% and 100%")]
    public decimal AlertThreshold { get; set; } = 0.8m;
    
    public List<BudgetCategoryDto>? Categories { get; set; }
}

public class BudgetDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public BudgetPeriod Period { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool AlertEnabled { get; set; }
    public decimal AlertThreshold { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal PercentageUsed { get; set; }
    public bool IsOverBudget { get; set; }
    public bool IsAlertTriggered { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<BudgetCategoryDto> Categories { get; set; } = new();
}

public class BudgetCategoryDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryColor { get; set; }
    public string? CategoryIcon { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal PercentageUsed { get; set; }
    public bool IsOverBudget { get; set; }
}

public class BudgetSummaryDto
{
    public decimal TotalBudgetAmount { get; set; }
    public decimal TotalSpentAmount { get; set; }
    public decimal TotalRemainingAmount { get; set; }
    public decimal OverallPercentageUsed { get; set; }
    public int TotalBudgets { get; set; }
    public int ActiveBudgets { get; set; }
    public int OverBudgetCount { get; set; }
    public int AlertTriggeredCount { get; set; }
}