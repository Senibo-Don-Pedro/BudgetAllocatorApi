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
    private readonly AppDbContext _db;
    private readonly ICurrencyExchangeService _currencyExchangeService;

    public IncomeProfileService(AppDbContext db, ICurrencyExchangeService currencyExchangeService)
    {
        _db = db;
        _currencyExchangeService = currencyExchangeService;
    }

    public async Task<BaseResponse<IncomeProfileResponse>> CreateAsync(CreateIncomeProfileRequest request)
    {
        // 1. Business rule: Total percentage cannot exceed 100%
        var totalPercentage = request.Categories.Sum(c => c.Percentage);

        if (totalPercentage > 100m)
        {
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

        return BaseResponse<IncomeProfileResponse>.Ok(MapToResponse(profile), "Income profile created successfully.");
    }

    public async Task<BaseResponse<List<IncomeProfileResponse>>> GetAllAsync()
    {
        // AsNoTracking() = Read-only optimization (like @Transactional(readOnly = true) in Spring)
        // Include() = Eagerly joins the BudgetCategories table in SQL
        var profiles = await _db.IncomeProfiles
            .AsNoTracking()
            .Include(p => p.Categories)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync();

        var responseList = profiles.Select(MapToResponse).ToList();

        return BaseResponse<List<IncomeProfileResponse>>.Ok(responseList);
    }

    public async Task<BaseResponse<IncomeProfileResponse>> GetByIdAsync(Guid id)
    {
        var profile = await _db.IncomeProfiles
            .AsNoTracking()
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (profile is null)
        {
            return BaseResponse<IncomeProfileResponse>.Error("Income profile not found.");
        }

        return BaseResponse<IncomeProfileResponse>.Ok(MapToResponse(profile));
    }

    public async Task<BaseResponse<bool>> DeleteAsync(Guid id)
    {
        var profile = await _db.IncomeProfiles.FirstOrDefaultAsync(p => p.Id == id);
        if (profile is null)
        {
            return BaseResponse<bool>.Error("Income profile not found.", false);
        }

        _db.IncomeProfiles.Remove(profile);
        await _db.SaveChangesAsync();

        return BaseResponse<bool>.Ok(true, "Income profile deleted successfully.");
    }

    public async Task<BaseResponse<ConvertedIncomeProfileResponse>> GetConvertedByIdAsync(Guid id, CurrencyEnum targetCurrency)
    {
        var profile = await _db.IncomeProfiles
            .AsNoTracking()
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (profile is null)
        {
            return BaseResponse<ConvertedIncomeProfileResponse>.Error("Income profile not found.");
        }

        var rate = await _currencyExchangeService.GetExchangeRateAsync(profile.Currency, targetCurrency);

        if (rate is null)
        {
            return BaseResponse<ConvertedIncomeProfileResponse>.Error(
                $"Could not fetch exchange rate from {profile.Currency} to {targetCurrency}.");
        }

        var converted = new ConvertedIncomeProfileResponse
        {
            ProfileId = profile.Id,
            SourceName = profile.SourceName,
            OriginalCurrency = profile.Currency,
            OriginalMonthlyNetIncome = profile.MonthlyNetIncome,
            TargetCurrency = targetCurrency ,
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

        return BaseResponse<ConvertedIncomeProfileResponse>.Ok(converted);
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