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
public class GoalsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GoalsController> _logger;

    public GoalsController(ApplicationDbContext context, ILogger<GoalsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    [HttpGet]
    public async Task<ActionResult<List<GoalDto>>> GetGoals([FromQuery] GoalStatus? status = null)
    {
        try
        {
            var userId = GetUserId();
            var query = _context.Goals
                .Where(g => g.UserId == userId)
                .Include(g => g.Contributions.OrderByDescending(c => c.Date).Take(5))
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(g => g.Status == status.Value);

            var goals = await query
                .OrderBy(g => g.TargetDate)
                .ToListAsync();

            var goalDtos = goals.Select(g => new GoalDto
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                TargetAmount = g.TargetAmount,
                CurrentAmount = g.CurrentAmount,
                RemainingAmount = g.TargetAmount - g.CurrentAmount,
                PercentageComplete = g.TargetAmount > 0 ? (g.CurrentAmount / g.TargetAmount) * 100 : 0,
                TargetDate = g.TargetDate,
                DaysRemaining = (g.TargetDate - DateTime.Now).Days,
                Type = g.Type,
                TypeName = g.Type.ToString(),
                Status = g.Status,
                StatusName = g.Status.ToString(),
                Color = g.Color,
                Icon = g.Icon,
                IsOnTrack = CalculateIsOnTrack(g),
                RecommendedMonthlyContribution = CalculateRecommendedMonthlyContribution(g),
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt,
                RecentContributions = g.Contributions.Select(c => new GoalContributionDto
                {
                    Id = c.Id,
                    Amount = c.Amount,
                    Notes = c.Notes,
                    Date = c.Date
                }).ToList()
            }).ToList();

            return Ok(goalDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving goals");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GoalDto>> GetGoal(int id)
    {
        try
        {
            var userId = GetUserId();
            var goal = await _context.Goals
                .Where(g => g.Id == id && g.UserId == userId)
                .Include(g => g.Contributions.OrderByDescending(c => c.Date))
                .FirstOrDefaultAsync();

            if (goal == null)
                return NotFound(new { message = "Goal not found" });

            var goalDto = new GoalDto
            {
                Id = goal.Id,
                Name = goal.Name,
                Description = goal.Description,
                TargetAmount = goal.TargetAmount,
                CurrentAmount = goal.CurrentAmount,
                RemainingAmount = goal.TargetAmount - goal.CurrentAmount,
                PercentageComplete = goal.TargetAmount > 0 ? (goal.CurrentAmount / goal.TargetAmount) * 100 : 0,
                TargetDate = goal.TargetDate,
                DaysRemaining = (goal.TargetDate - DateTime.Now).Days,
                Type = goal.Type,
                TypeName = goal.Type.ToString(),
                Status = goal.Status,
                StatusName = goal.Status.ToString(),
                Color = goal.Color,
                Icon = goal.Icon,
                IsOnTrack = CalculateIsOnTrack(goal),
                RecommendedMonthlyContribution = CalculateRecommendedMonthlyContribution(goal),
                CreatedAt = goal.CreatedAt,
                UpdatedAt = goal.UpdatedAt,
                RecentContributions = goal.Contributions.Select(c => new GoalContributionDto
                {
                    Id = c.Id,
                    Amount = c.Amount,
                    Notes = c.Notes,
                    Date = c.Date
                }).ToList()
            };

            return Ok(goalDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving goal {GoalId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<GoalDto>> CreateGoal([FromBody] CreateGoalDto dto)
    {
        try
        {
            var userId = GetUserId();

            if (dto.TargetDate <= DateTime.Now)
                return BadRequest(new { message = "Target date must be in the future" });

            var goal = new Goal
            {
                Name = dto.Name,
                Description = dto.Description,
                TargetAmount = dto.TargetAmount,
                CurrentAmount = dto.CurrentAmount,
                TargetDate = dto.TargetDate,
                Type = dto.Type,
                Color = dto.Color,
                Icon = dto.Icon,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Goals.Add(goal);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Goal created: {GoalId} for user {UserId}", goal.Id, userId);
            
            // Return the created goal
            var result = await GetGoal(goal.Id);
            return CreatedAtAction(nameof(GetGoal), new { id = goal.Id }, result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating goal");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<GoalDto>> UpdateGoal(int id, [FromBody] UpdateGoalDto dto)
    {
        try
        {
            var userId = GetUserId();
            var goal = await _context.Goals
                .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

            if (goal == null)
                return NotFound(new { message = "Goal not found" });

            if (dto.TargetDate <= DateTime.Now)
                return BadRequest(new { message = "Target date must be in the future" });

            goal.Name = dto.Name;
            goal.Description = dto.Description;
            goal.TargetAmount = dto.TargetAmount;
            goal.TargetDate = dto.TargetDate;
            goal.Type = dto.Type;
            goal.Status = dto.Status;
            goal.Color = dto.Color;
            goal.Icon = dto.Icon;
            goal.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Goal updated: {GoalId}", id);
            
            // Return the updated goal
            var result = await GetGoal(goal.Id);
            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating goal {GoalId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{id}/contributions")]
    public async Task<ActionResult<GoalDto>> AddContribution(int id, [FromBody] CreateGoalContributionDto dto)
    {
        try
        {
            var userId = GetUserId();
            var goal = await _context.Goals
                .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

            if (goal == null)
                return NotFound(new { message = "Goal not found" });

            var contribution = new GoalContribution
            {
                GoalId = goal.Id,
                Amount = dto.Amount,
                Notes = dto.Notes,
                Date = dto.Date
            };

            _context.GoalContributions.Add(contribution);

            // Update goal current amount
            goal.CurrentAmount += dto.Amount;
            goal.UpdatedAt = DateTime.UtcNow;

            // Check if goal is completed
            if (goal.CurrentAmount >= goal.TargetAmount && goal.Status == GoalStatus.Active)
            {
                goal.Status = GoalStatus.Completed;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Contribution added to goal {GoalId}: {Amount}", id, dto.Amount);
            
            // Return the updated goal
            var result = await GetGoal(goal.Id);
            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding contribution to goal {GoalId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGoal(int id)
    {
        try
        {
            var userId = GetUserId();
            var goal = await _context.Goals
                .Include(g => g.Contributions)
                .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

            if (goal == null)
                return NotFound(new { message = "Goal not found" });

            // Remove all contributions first
            _context.GoalContributions.RemoveRange(goal.Contributions);
            _context.Goals.Remove(goal);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Goal deleted: {GoalId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting goal {GoalId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("summary")]
    public async Task<ActionResult<GoalSummaryDto>> GetGoalSummary()
    {
        try
        {
            var userId = GetUserId();
            var goals = await _context.Goals
                .Where(g => g.UserId == userId)
                .ToListAsync();

            var summary = new GoalSummaryDto
            {
                TotalTargetAmount = goals.Sum(g => g.TargetAmount),
                TotalCurrentAmount = goals.Sum(g => g.CurrentAmount),
                TotalRemainingAmount = goals.Sum(g => g.TargetAmount - g.CurrentAmount),
                OverallPercentageComplete = goals.Sum(g => g.TargetAmount) > 0 ? 
                    (goals.Sum(g => g.CurrentAmount) / goals.Sum(g => g.TargetAmount)) * 100 : 0,
                TotalGoals = goals.Count,
                ActiveGoals = goals.Count(g => g.Status == GoalStatus.Active),
                CompletedGoals = goals.Count(g => g.Status == GoalStatus.Completed),
                OnTrackGoals = goals.Count(g => g.Status == GoalStatus.Active && CalculateIsOnTrack(g)),
                BehindScheduleGoals = goals.Count(g => g.Status == GoalStatus.Active && !CalculateIsOnTrack(g))
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving goal summary");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    private bool CalculateIsOnTrack(Goal goal)
    {
        if (goal.Status != GoalStatus.Active)
            return true;

        var daysTotal = (goal.TargetDate - goal.CreatedAt).Days;
        var daysElapsed = (DateTime.Now - goal.CreatedAt).Days;

        if (daysTotal <= 0)
            return goal.CurrentAmount >= goal.TargetAmount;

        var expectedProgress = (decimal)daysElapsed / daysTotal;
        var actualProgress = goal.TargetAmount > 0 ? goal.CurrentAmount / goal.TargetAmount : 0;

        return actualProgress >= expectedProgress;
    }

    private decimal CalculateRecommendedMonthlyContribution(Goal goal)
    {
        if (goal.Status != GoalStatus.Active || goal.TargetDate <= DateTime.Now)
            return 0;

        var remainingAmount = goal.TargetAmount - goal.CurrentAmount;
        var remainingMonths = (decimal)((goal.TargetDate.Year - DateTime.Now.Year) * 12 + goal.TargetDate.Month - DateTime.Now.Month);

        if (remainingMonths <= 0)
            return remainingAmount;

        return Math.Max(0, remainingAmount / remainingMonths);
    }
}