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
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(ApplicationDbContext context, ILogger<CategoriesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories([FromQuery] CategoryType? type = null)
    {
        try
        {
            var userId = GetUserId();
            var query = _context.Categories
                .Where(c => c.UserId == userId || c.UserId == "seed") // Include default categories
                .Include(c => c.Transactions)
                .AsQueryable();

            if (type.HasValue)
                query = query.Where(c => c.Type == type.Value);

            var categories = await query
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    Color = c.Color,
                    Icon = c.Icon,
                    Type = c.Type,
                    TypeName = c.Type.ToString(),
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    TransactionCount = c.Transactions.Count(t => t.UserId == userId),
                    TotalAmount = c.Transactions.Where(t => t.UserId == userId).Sum(t => t.Amount)
                })
                .ToListAsync();

            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetCategory(int id)
    {
        try
        {
            var userId = GetUserId();
            var category = await _context.Categories
                .Where(c => c.Id == id && (c.UserId == userId || c.UserId == "seed"))
                .Include(c => c.Transactions)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    Color = c.Color,
                    Icon = c.Icon,
                    Type = c.Type,
                    TypeName = c.Type.ToString(),
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    TransactionCount = c.Transactions.Count(t => t.UserId == userId),
                    TotalAmount = c.Transactions.Where(t => t.UserId == userId).Sum(t => t.Amount)
                })
                .FirstOrDefaultAsync();

            if (category == null)
                return NotFound(new { message = "Category not found" });

            return Ok(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category {CategoryId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryDto dto)
    {
        try
        {
            var userId = GetUserId();
            
            // Check if category with same name already exists for user
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == dto.Name.ToLower() && 
                                         c.UserId == userId && c.Type == dto.Type);
            
            if (existingCategory != null)
                return BadRequest(new { message = "Category with this name already exists" });

            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description,
                Color = dto.Color,
                Icon = dto.Icon,
                Type = dto.Type,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var categoryDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Color = category.Color,
                Icon = category.Icon,
                Type = category.Type,
                TypeName = category.Type.ToString(),
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt,
                TransactionCount = 0,
                TotalAmount = 0
            };

            _logger.LogInformation("Category created: {CategoryId} for user {UserId}", category.Id, userId);
            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, categoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(int id, [FromBody] UpdateCategoryDto dto)
    {
        try
        {
            var userId = GetUserId();
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (category == null)
                return NotFound(new { message = "Category not found" });

            // Check if another category with same name exists
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == dto.Name.ToLower() && 
                                         c.UserId == userId && c.Type == dto.Type && c.Id != id);
            
            if (existingCategory != null)
                return BadRequest(new { message = "Category with this name already exists" });

            category.Name = dto.Name;
            category.Description = dto.Description;
            category.Color = dto.Color;
            category.Icon = dto.Icon;
            category.Type = dto.Type;
            category.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            var categoryDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Color = category.Color,
                Icon = category.Icon,
                Type = category.Type,
                TypeName = category.Type.ToString(),
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt,
                TransactionCount = await _context.Transactions.CountAsync(t => t.CategoryId == category.Id && t.UserId == userId),
                TotalAmount = await _context.Transactions.Where(t => t.CategoryId == category.Id && t.UserId == userId).SumAsync(t => t.Amount)
            };

            _logger.LogInformation("Category updated: {CategoryId}", id);
            return Ok(categoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {CategoryId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        try
        {
            var userId = GetUserId();
            var category = await _context.Categories
                .Include(c => c.Transactions)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (category == null)
                return NotFound(new { message = "Category not found" });

            if (category.Transactions.Any(t => t.UserId == userId))
                return BadRequest(new { message = "Cannot delete category with existing transactions" });

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Category deleted: {CategoryId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {CategoryId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TagsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TagsController> _logger;

    public TagsController(ApplicationDbContext context, ILogger<TagsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    [HttpGet]
    public async Task<ActionResult<List<TagDto>>> GetTags()
    {
        try
        {
            var userId = GetUserId();
            var tags = await _context.Tags
                .Where(t => t.UserId == userId)
                .Include(t => t.TransactionTags)
                .OrderBy(t => t.Name)
                .Select(t => new TagDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Color = t.Color,
                    CreatedAt = t.CreatedAt,
                    UsageCount = t.TransactionTags.Count
                })
                .ToListAsync();

            return Ok(tags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tags");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<TagDto>> CreateTag([FromBody] CreateTagDto dto)
    {
        try
        {
            var userId = GetUserId();
            
            // Check if tag with same name already exists for user
            var existingTag = await _context.Tags
                .FirstOrDefaultAsync(t => t.Name.ToLower() == dto.Name.ToLower() && t.UserId == userId);
            
            if (existingTag != null)
                return BadRequest(new { message = "Tag with this name already exists" });

            var tag = new Tag
            {
                Name = dto.Name,
                Color = dto.Color,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();

            var tagDto = new TagDto
            {
                Id = tag.Id,
                Name = tag.Name,
                Color = tag.Color,
                CreatedAt = tag.CreatedAt,
                UsageCount = 0
            };

            _logger.LogInformation("Tag created: {TagId} for user {UserId}", tag.Id, userId);
            return CreatedAtAction(nameof(GetTags), tagDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tag");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTag(int id)
    {
        try
        {
            var userId = GetUserId();
            var tag = await _context.Tags
                .Include(t => t.TransactionTags)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (tag == null)
                return NotFound(new { message = "Tag not found" });

            // Remove all transaction tag relationships
            _context.TransactionTags.RemoveRange(tag.TransactionTags);
            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Tag deleted: {TagId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tag {TagId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}