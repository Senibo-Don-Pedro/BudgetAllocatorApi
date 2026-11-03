using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BudgetAllocator.Api.Interfaces;
using BudgetAllocator.Api.Models.DTO.Request;
using BudgetAllocator.Api.Models.DTO.Response;
using BudgetAllocator.Api.Models.DTO.Response.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetAllocator.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EndpointSummary("Login to receive an Access Token and Refresh Token")]
    public async Task<ActionResult<BaseResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EndpointSummary("Exchange a valid Refresh Token for a new Access Token + rotated Refresh Token")]
    public async Task<ActionResult<BaseResponse<AuthResponse>>> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    [HttpPost("revoke")]
    [Authorize]
    [EndpointSummary("Revoke a Refresh Token (Logout)")]
    public async Task<ActionResult<BaseResponse<bool>>> Revoke([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RevokeTokenAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    [EndpointSummary("Get current logged-in user claims from JWT")]
    public ActionResult<BaseResponse<object>> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);

        return Ok(BaseResponse<object>.Ok(new
        {
            UserId = userId,
            Email = email,
            Role = role
        }));
    }
}