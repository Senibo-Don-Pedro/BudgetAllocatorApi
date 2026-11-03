using System.Text.Json.Serialization;

namespace BudgetAllocator.Api.Models.Enum;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CurrencyEnum
{
    NGN,
    USD,
    GBP,
    EUR,
    CAD,
    GHS,
    KES,
    ZAR
}