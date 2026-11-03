using System.ComponentModel.DataAnnotations;

namespace BudgetAllocator.Api.Models.DTO.Request;

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token is required.")]
    public string RefreshToken { get; set; } = string.Empty;
}