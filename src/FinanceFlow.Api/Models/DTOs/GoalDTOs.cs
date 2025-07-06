using System.ComponentModel.DataAnnotations;
using FinanceFlow.Api.Models.Financial;

namespace FinanceFlow.Api.Models.DTOs;

public class CreateGoalDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Target amount must be greater than 0")]
    public decimal TargetAmount { get; set; }
    
    public decimal CurrentAmount { get; set; } = 0;
    
    [Required]
    public DateTime TargetDate { get; set; }
    
    [Required]
    public GoalType Type { get; set; }
    
    [MaxLength(50)]
    public string? Color { get; set; }
    
    [MaxLength(50)]
    public string? Icon { get; set; }
}

public class UpdateGoalDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Target amount must be greater than 0")]
    public decimal TargetAmount { get; set; }
    
    [Required]
    public DateTime TargetDate { get; set; }
    
    [Required]
    public GoalType Type { get; set; }
    
    [Required]
    public GoalStatus Status { get; set; }
    
    [MaxLength(50)]
    public string? Color { get; set; }
    
    [MaxLength(50)]
    public string? Icon { get; set; }
}

public class GoalDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal PercentageComplete { get; set; }
    public DateTime TargetDate { get; set; }
    public int DaysRemaining { get; set; }
    public GoalType Type { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public GoalStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public bool IsOnTrack { get; set; }
    public decimal RecommendedMonthlyContribution { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<GoalContributionDto> RecentContributions { get; set; } = new();
}

public class GoalContributionDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public DateTime Date { get; set; }
}

public class CreateGoalContributionDto
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    [Required]
    public DateTime Date { get; set; }
}

public class GoalSummaryDto
{
    public decimal TotalTargetAmount { get; set; }
    public decimal TotalCurrentAmount { get; set; }
    public decimal TotalRemainingAmount { get; set; }
    public decimal OverallPercentageComplete { get; set; }
    public int TotalGoals { get; set; }
    public int ActiveGoals { get; set; }
    public int CompletedGoals { get; set; }
    public int OnTrackGoals { get; set; }
    public int BehindScheduleGoals { get; set; }
}