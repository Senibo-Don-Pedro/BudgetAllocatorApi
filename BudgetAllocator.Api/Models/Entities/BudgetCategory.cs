namespace BudgetAllocator.Api.Models.Entities;

public class BudgetCategory
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Percentage { get; set; }

    public decimal AllocatedAmount { get; set; }

    // Foreign Key property
    public Guid IncomeProfileId { get; set; }

    // Reference navigation back to parent
    public IncomeProfile IncomeProfile { get; set; } = null!;
}