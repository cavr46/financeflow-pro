using FinanceFlow.Api.Models;
using FinanceFlow.Api.Models.Financial;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Api.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Financial DbSets
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Budget> Budgets { get; set; }
    public DbSet<BudgetCategory> BudgetCategories { get; set; }
    public DbSet<Goal> Goals { get; set; }
    public DbSet<GoalContribution> GoalContributions { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<TransactionTag> TransactionTags { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Configure ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName)
                .HasMaxLength(100);
            
            entity.Property(e => e.LastName)
                .HasMaxLength(100);
            
            entity.Property(e => e.DateCreated)
                .HasDefaultValueSql("GETUTCDATE()");
        });

        // Configure Account
        builder.Entity<Account>(entity =>
        {
            entity.Property(e => e.Balance)
                .HasColumnType("decimal(18,2)");
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Transaction
        builder.Entity<Transaction>(entity =>
        {
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)");
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Account)
                .WithMany(a => a.Transactions)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ToAccount)
                .WithMany()
                .HasForeignKey(e => e.ToAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Category
        builder.Entity<Category>(entity =>
        {
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Budget
        builder.Entity<Budget>(entity =>
        {
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)");
            
            entity.Property(e => e.AlertThreshold)
                .HasColumnType("decimal(5,2)");
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure BudgetCategory
        builder.Entity<BudgetCategory>(entity =>
        {
            entity.Property(e => e.AllocatedAmount)
                .HasColumnType("decimal(18,2)");
            
            entity.Property(e => e.SpentAmount)
                .HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.Budget)
                .WithMany(b => b.BudgetCategories)
                .HasForeignKey(e => e.BudgetId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Goal
        builder.Entity<Goal>(entity =>
        {
            entity.Property(e => e.TargetAmount)
                .HasColumnType("decimal(18,2)");
            
            entity.Property(e => e.CurrentAmount)
                .HasColumnType("decimal(18,2)");
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure GoalContribution
        builder.Entity<GoalContribution>(entity =>
        {
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.Goal)
                .WithMany(g => g.Contributions)
                .HasForeignKey(e => e.GoalId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Tag
        builder.Entity<Tag>(entity =>
        {
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure TransactionTag (many-to-many)
        builder.Entity<TransactionTag>(entity =>
        {
            entity.HasKey(tt => new { tt.TransactionId, tt.TagId });

            entity.HasOne(tt => tt.Transaction)
                .WithMany(t => t.TransactionTags)
                .HasForeignKey(tt => tt.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(tt => tt.Tag)
                .WithMany(t => t.TransactionTags)
                .HasForeignKey(tt => tt.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed default categories
        SeedDefaultCategories(builder);
    }

    private void SeedDefaultCategories(ModelBuilder builder)
    {
        // Note: In a real application, you'd seed these for each user
        // For demo purposes, we'll create some default categories
        builder.Entity<Category>().HasData(
            // Income Categories
            new Category { Id = 1, Name = "Salary", Type = CategoryType.Income, Color = "#4CAF50", Icon = "fas fa-briefcase", UserId = "seed", CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Name = "Freelance", Type = CategoryType.Income, Color = "#2196F3", Icon = "fas fa-laptop", UserId = "seed", CreatedAt = DateTime.UtcNow },
            new Category { Id = 3, Name = "Investment", Type = CategoryType.Income, Color = "#FF9800", Icon = "fas fa-chart-line", UserId = "seed", CreatedAt = DateTime.UtcNow },
            
            // Expense Categories
            new Category { Id = 4, Name = "Food & Dining", Type = CategoryType.Expense, Color = "#F44336", Icon = "fas fa-utensils", UserId = "seed", CreatedAt = DateTime.UtcNow },
            new Category { Id = 5, Name = "Transportation", Type = CategoryType.Expense, Color = "#9C27B0", Icon = "fas fa-car", UserId = "seed", CreatedAt = DateTime.UtcNow },
            new Category { Id = 6, Name = "Shopping", Type = CategoryType.Expense, Color = "#E91E63", Icon = "fas fa-shopping-bag", UserId = "seed", CreatedAt = DateTime.UtcNow },
            new Category { Id = 7, Name = "Entertainment", Type = CategoryType.Expense, Color = "#673AB7", Icon = "fas fa-film", UserId = "seed", CreatedAt = DateTime.UtcNow },
            new Category { Id = 8, Name = "Utilities", Type = CategoryType.Expense, Color = "#607D8B", Icon = "fas fa-bolt", UserId = "seed", CreatedAt = DateTime.UtcNow },
            new Category { Id = 9, Name = "Healthcare", Type = CategoryType.Expense, Color = "#009688", Icon = "fas fa-heartbeat", UserId = "seed", CreatedAt = DateTime.UtcNow },
            new Category { Id = 10, Name = "Education", Type = CategoryType.Expense, Color = "#795548", Icon = "fas fa-graduation-cap", UserId = "seed", CreatedAt = DateTime.UtcNow }
        );
    }
}