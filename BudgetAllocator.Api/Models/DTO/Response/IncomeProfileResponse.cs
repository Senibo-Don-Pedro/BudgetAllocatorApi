using BudgetAllocator.Api.Models.Enum;

namespace BudgetAllocator.Api.Models.DTO.Response;

public class IncomeProfileResponse
{
    public Guid Id { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public decimal MonthlyNetIncome { get; set; }
    public CurrencyEnum Currency { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<BudgetCategoryResponse> Categories { get; set; } = new();
}

public class BudgetCategoryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public decimal AllocatedAmount { get; set; }
}