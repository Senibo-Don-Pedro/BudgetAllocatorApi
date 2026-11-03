using BudgetAllocator.Api.Data;
using BudgetAllocator.Api.Interfaces;
using BudgetAllocator.Api.Models.Config;
using BudgetAllocator.Api.Models.DTO.Response;
using BudgetAllocator.Api.Models.Enum;
using Microsoft.Extensions.Options;

namespace BudgetAllocator.Api.Services;

public class CurrencyExchangeService : ICurrencyExchangeService
{
    private readonly HttpClient _httpClient;
    private readonly ExchangeRateApiConfig _config;

    public CurrencyExchangeService(HttpClient httpClient, IOptions<ExchangeRateApiConfig> config)
    {
        _httpClient = httpClient;
        _config = config.Value;
    }

    public async Task<decimal?> GetExchangeRateAsync(CurrencyEnum baseCurrency, CurrencyEnum targetCurrency)
    {
        var url = $"{_config.BaseUrl}{baseCurrency}";
        
        // GetFromJsonAsync fetches the URL and deserializes JSON in one step!
        var response = await _httpClient.GetFromJsonAsync<ExternalExchangeRateResponse>(url);

        if (response is null || response.Result != "success")
        {
            return null;
        }

        

        if (response.Rates.TryGetValue(targetCurrency.ToString(), out var rate))
        {
            return rate;
        }

        return null;
    }
}