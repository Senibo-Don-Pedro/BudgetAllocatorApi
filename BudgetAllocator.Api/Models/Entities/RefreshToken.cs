using System.ComponentModel.DataAnnotations;
using BudgetAllocator.Api.Models.Enum;

namespace BudgetAllocator.Api.Entities;

public class RefreshToken
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public RoleEnum Role { get; set; } = RoleEnum.User;

    public DateTime ExpiresAtUtc { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}