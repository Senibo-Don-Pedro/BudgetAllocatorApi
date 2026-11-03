using System.ComponentModel.DataAnnotations;
using BudgetAllocator.Api.Models.Enum;

namespace BudgetAllocator.Api.Models.DTO.Request;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "engineer@lagos.dev";

    [Required]
    public string Password { get; set; } = "Password123!";

    /// <summary>
    /// Pass "Admin" or "User" to test Role-Based Authorization!
    /// </summary>
    public RoleEnum Role { get; set; } = RoleEnum.Admin;
}