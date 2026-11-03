using BudgetAllocator.Api.Models.Enum;

namespace BudgetAllocator.Api.Models.DTO.Response;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    
    public string RefreshToken { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public RoleEnum Role { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    
    public DateTime RefreshTokenExpiresAtUtc { get; set; }
}