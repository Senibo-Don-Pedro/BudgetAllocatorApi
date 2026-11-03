using BudgetAllocator.Api.Models.DTO.Request;
using BudgetAllocator.Api.Models.DTO.Response;
using BudgetAllocator.Api.Models.DTO.Response.Generic;
using BudgetAllocator.Api.Models.Enum;

namespace BudgetAllocator.Api.Interfaces;

public interface IIncomeProfileService
{
    Task<BaseResponse<IncomeProfileResponse>> CreateAsync(CreateIncomeProfileRequest request);

    Task<BaseResponse<List<IncomeProfileResponse>>> GetAllAsync();

    Task<BaseResponse<IncomeProfileResponse>> GetByIdAsync(Guid id);

    Task<BaseResponse<bool>> DeleteAsync(Guid id);

    Task<BaseResponse<ConvertedIncomeProfileResponse>> GetConvertedByIdAsync(Guid id, CurrencyEnum targetCurrency);
}