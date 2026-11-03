using BudgetAllocator.Api.Models.Enum;

namespace BudgetAllocator.Api.Models.Entities;

public class IncomeProfile
{
    public Guid Id { get; set; }

    public string SourceName { get; set; } = string.Empty;

    public decimal MonthlyNetIncome { get; set; }

    public CurrencyEnum Currency { get; set; } = CurrencyEnum.NGN;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<BudgetCategory> Categories { get; set; } = new();
}