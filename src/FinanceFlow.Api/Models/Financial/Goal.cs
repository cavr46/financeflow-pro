using System.ComponentModel.DataAnnotations;

namespace FinanceFlow.Api.Models.Financial;

public class Goal
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    public decimal TargetAmount { get; set; }
    
    public decimal CurrentAmount { get; set; } = 0;
    
    [Required]
    public DateTime TargetDate { get; set; }
    
    public GoalType Type { get; set; }
    
    public GoalStatus Status { get; set; } = GoalStatus.Active;
    
    [MaxLength(50)]
    public string? Color { get; set; }
    
    [MaxLength(50)]
    public string? Icon { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public ICollection<GoalContribution> Contributions { get; set; } = new List<GoalContribution>();
}

public class GoalContribution
{
    public int Id { get; set; }
    
    [Required]
    public int GoalId { get; set; }
    
    [Required]
    public decimal Amount { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    [Required]
    public DateTime Date { get; set; }
    
    // Navigation properties
    public Goal Goal { get; set; } = null!;
}

public enum GoalType
{
    Savings = 0,
    DebtPayoff = 1,
    Investment = 2,
    Purchase = 3,
    Emergency = 4,
    Other = 5
}

public enum GoalStatus
{
    Active = 0,
    Completed = 1,
    Paused = 2,
    Cancelled = 3
}