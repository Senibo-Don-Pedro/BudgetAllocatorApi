using BudgetAllocator.Api.Models.DTO.Request;
using BudgetAllocator.Api.Models.DTO.Response;
using BudgetAllocator.Api.Models.DTO.Response.Generic;

namespace BudgetAllocator.Api.Interfaces;

public interface IAuthService
{
    Task<BaseResponse<AuthResponse>> LoginAsync(LoginRequest request);
    Task<BaseResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request);
    Task<BaseResponse<bool>> RevokeTokenAsync(RefreshTokenRequest request);
}