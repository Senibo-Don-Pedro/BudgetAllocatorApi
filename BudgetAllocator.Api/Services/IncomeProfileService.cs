using BudgetAllocator.Api.Data;
using BudgetAllocator.Api.Interfaces;
using BudgetAllocator.Api.Models.DTO.Request;
using BudgetAllocator.Api.Models.DTO.Response;
using BudgetAllocator.Api.Models.DTO.Response.Generic;
using BudgetAllocator.Api.Models.Entities;
using BudgetAllocator.Api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace BudgetAllocator.Api.Services;

public class IncomeProfileService : IIncomeProfileService
{
    private readonly ICurrencyExchangeService _currencyExchangeService;
    private readonly AppDbContext _db;
    private readonly ILogger<IncomeProfileService> _logger;

    public IncomeProfileService(
        AppDbContext db,
        ICurrencyExchangeService currencyExchangeService,
        ILogger<IncomeProfileService> logger)
    {
        _db = db;
        _currencyExchangeService = currencyExchangeService;
        _logger = logger;
    }

    public async Task<BaseResponse<IncomeProfileResponse>> CreateAsync(CreateIncomeProfileRequest request)
    {
        // 1. Business rule: Total percentage cannot exceed 100%
        var totalPercentage = request.Categories.Sum(c => c.Percentage);

        if (totalPercentage > 100m)

        {
            _logger.LogWarning("Budget allocation rejected for {SourceName}: total percentage was {TotalPercentage}%",
                request.SourceName, totalPercentage);

            return BaseResponse<IncomeProfileResponse>.Error(
                $"Total category percentage ({totalPercentage}%) cannot exceed 100%.");
        }

        // 2. Map Request DTO to Entity & calculate each category's AllocatedAmount
        var profile = new IncomeProfile
        {
            Id = Guid.NewGuid(),
            SourceName = request.SourceName,
            MonthlyNetIncome = request.MonthlyNetIncome,
            Currency = request.Currency,
            CreatedAtUtc = DateTime.UtcNow,
            Categories = request.Categories.Select(c => new BudgetCategory
            {
                Id = Guid.NewGuid(),
                Name = c.Name,
                Percentage = c.Percentage,
                AllocatedAmount = Math.Round(request.MonthlyNetIncome * (c.Percentage / 100m), 2)
            }).ToList()
        };

        // 3. Persist to SQL Server
        _db.IncomeProfiles.Add(profile);

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Created IncomeProfile {ProfileId} ({SourceName}) with {CategoryCount} categories totaling {MonthlyNetIncome} {Currency}",
            profile.Id, profile.SourceName, profile.Categories.Count, profile.MonthlyNetIncome, profile.Currency);

        return BaseResponse<IncomeProfileResponse>.Ok(MapToResponse(profile), "Income profile created successfully.");
    }

    public async Task<BaseResponse<IncomeProfileResponse>> GetByIdAsync(Guid id)
    {
        var profile = await _db.IncomeProfiles
            .AsNoTracking()
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (profile is null) return BaseResponse<IncomeProfileResponse>.Error("Income profile not found.");

        return BaseResponse<IncomeProfileResponse>.Ok(MapToResponse(profile));
    }

    public async Task<BaseResponse<bool>> DeleteAsync(Guid id)
    {
        var profile = await _db.IncomeProfiles.FirstOrDefaultAsync(p => p.Id == id);
        if (profile is null) return BaseResponse<bool>.Error("Income profile not found.");

        _db.IncomeProfiles.Remove(profile);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted IncomeProfile {ProfileId}", id);

        return BaseResponse<bool>.Ok(true, "Income profile deleted successfully.");
    }

    public async Task<BaseResponse<ConvertedIncomeProfileResponse>> GetConvertedByIdAsync(Guid id,
        CurrencyEnum targetCurrency)
    {
        var profile = await _db.IncomeProfiles
            .AsNoTracking()
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (profile is null) return BaseResponse<ConvertedIncomeProfileResponse>.Error("Income profile not found.");

        var rate = await _currencyExchangeService.GetExchangeRateAsync(profile.Currency, targetCurrency);

        if (rate is null)
            return BaseResponse<ConvertedIncomeProfileResponse>.Error(
                $"Could not fetch exchange rate from {profile.Currency} to {targetCurrency}.");

        var converted = new ConvertedIncomeProfileResponse
        {
            ProfileId = profile.Id,
            SourceName = profile.SourceName,
            OriginalCurrency = profile.Currency,
            OriginalMonthlyNetIncome = profile.MonthlyNetIncome,
            TargetCurrency = targetCurrency,
            ExchangeRate = rate.Value,
            ConvertedMonthlyNetIncome = Math.Round(profile.MonthlyNetIncome * rate.Value, 2),
            ConvertedCategories = profile.Categories.Select(c => new BudgetCategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Percentage = c.Percentage,
                AllocatedAmount = Math.Round(c.AllocatedAmount * rate.Value, 2)
            }).ToList()
        };

        _logger.LogInformation(
            "Converted IncomeProfile {ProfileId} from {OriginalCurrency} to {TargetCurrency} at rate {ExchangeRate}",
            profile.Id, profile.Currency, targetCurrency, rate.Value);

        return BaseResponse<ConvertedIncomeProfileResponse>.Ok(converted);
    }

    public async Task<BaseResponse<PagedData<IncomeProfileResponse>>> GetAllAsync(int pageNumber, int pageSize)
    {
        // Guard against invalid 0 or negative query params, and cap max pageSize at 50
        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = pageSize < 1 ? 10 : Math.Min(pageSize, 50);

        // 1. Base IQueryable (not executed yet!)
        var query = _db.IncomeProfiles.AsNoTracking();

        // 2. Execute SELECT COUNT(*) in SQL Server
        var totalCount = await query.CountAsync();

        // 3. Execute paginated SELECT with OFFSET and FETCH NEXT
        var profiles = await query
            .Include(p => p.Categories)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = profiles.Select(MapToResponse).ToList();

        var pagedData = new PagedData<IncomeProfileResponse>(
            items,
            totalCount,
            pageNumber,
            pageSize);

        return BaseResponse<PagedData<IncomeProfileResponse>>.Ok(pagedData);
    }

    private static IncomeProfileResponse MapToResponse(IncomeProfile profile)
    {
        return new IncomeProfileResponse
        {
            Id = profile.Id,
            SourceName = profile.SourceName,
            MonthlyNetIncome = profile.MonthlyNetIncome,
            Currency = profile.Currency,
            CreatedAtUtc = profile.CreatedAtUtc,
            Categories = profile.Categories.Select(c => new BudgetCategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Percentage = c.Percentage,
                AllocatedAmount = c.AllocatedAmount
            }).ToList()
        };
    }
}