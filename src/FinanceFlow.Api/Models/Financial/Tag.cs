using System.ComponentModel.DataAnnotations;

namespace FinanceFlow.Api.Models.Financial;

public class Tag
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? Color { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public ICollection<TransactionTag> TransactionTags { get; set; } = new List<TransactionTag>();
}

public class TransactionTag
{
    public int TransactionId { get; set; }
    public int TagId { get; set; }
    
    // Navigation properties
    public Transaction Transaction { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}