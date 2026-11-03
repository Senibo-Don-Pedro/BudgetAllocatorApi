using BudgetAllocator.Api.Models.Enum;

namespace BudgetAllocator.Api.Interfaces;

public interface ICurrencyExchangeService
{
    Task<decimal?> GetExchangeRateAsync(CurrencyEnum baseCurrency, CurrencyEnum targetCurrency);
}