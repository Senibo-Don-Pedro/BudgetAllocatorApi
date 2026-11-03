using System.Text.Json.Serialization;
using BudgetAllocator.Api.Models.Enum;

namespace BudgetAllocator.Api.Models.DTO.Response;

// 1. Matches external API JSON from https://open.er-api.com/v6/latest/NGN
public class ExternalExchangeRateResponse
{
    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;

    [JsonPropertyName("base_code")]
    public string BaseCode { get; set; } = string.Empty;

    [JsonPropertyName("rates")]
    public Dictionary<string, decimal> Rates { get; set; } = new();
}

// 2. Our API's Response DTO showing the converted profile
public class ConvertedIncomeProfileResponse
{
    public Guid ProfileId { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public CurrencyEnum OriginalCurrency { get; set; }
    public decimal OriginalMonthlyNetIncome { get; set; }
    public CurrencyEnum TargetCurrency { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal ConvertedMonthlyNetIncome { get; set; }
    public List<BudgetCategoryResponse> ConvertedCategories { get; set; } = new();
}