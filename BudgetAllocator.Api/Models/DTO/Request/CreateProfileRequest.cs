using System.ComponentModel.DataAnnotations;
using BudgetAllocator.Api.Models.Enum;

namespace BudgetAllocator.Api.Models.DTO.Request;

public class CreateIncomeProfileRequest
{
    [Required(ErrorMessage = "Source name is required.")]
    [MaxLength(100, ErrorMessage = "Source name cannot exceed 100 characters.")]
    public string SourceName { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Monthly net income must be greater than zero.")]
    public decimal MonthlyNetIncome { get; set; }

    [Required(ErrorMessage = "Currency is required.")]
    [MaxLength(10, ErrorMessage = "Currency cannot exceed 10 characters.")]
    public CurrencyEnum Currency { get; set; } = CurrencyEnum.NGN;

    [Required(ErrorMessage = "Categories are required.")]
    [MinLength(1, ErrorMessage = "At least one budget category is required.")]
    public List<CreateBudgetCategoryRequest> Categories { get; set; } = new();
}

public class CreateBudgetCategoryRequest
{
    [Required(ErrorMessage = "Category name is required.")]
    [MaxLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 100.00, ErrorMessage = "Percentage must be between 0.01 and 100.")]
    public decimal Percentage { get; set; }
}