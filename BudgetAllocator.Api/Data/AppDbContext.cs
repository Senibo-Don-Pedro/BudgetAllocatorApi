using BudgetAllocator.Api.Entities;
using BudgetAllocator.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetAllocator.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSets = Your SQL Server Tables
    public DbSet<IncomeProfile> IncomeProfiles { get; set; } = null!;
    public DbSet<BudgetCategory> BudgetCategories { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure IncomeProfile table
        var profile = modelBuilder.Entity<IncomeProfile>();

        profile.Property(x => x.SourceName).IsRequired().HasMaxLength(100);
        profile.Property(x => x.Currency).HasConversion<string>().IsRequired().HasMaxLength(10);
        profile.Property(x => x.MonthlyNetIncome).HasPrecision(18, 2);

        // Configure BudgetCategory table
        var category = modelBuilder.Entity<BudgetCategory>();
        category.Property(x => x.Name).IsRequired().HasMaxLength(100);
        category.Property(x => x.Percentage).HasPrecision(5, 2);
        category.Property(x => x.AllocatedAmount).HasPrecision(18, 2);
        
        // Configure RefreshToken table
        var refreshToken = modelBuilder.Entity<RefreshToken>();
        refreshToken.Property(x => x.Role).HasConversion<string>().HasMaxLength(50);
    }
}