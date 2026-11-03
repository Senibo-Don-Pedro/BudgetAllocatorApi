using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BudgetAllocator.Api.Data;
using BudgetAllocator.Api.Entities;
using BudgetAllocator.Api.Interfaces;
using BudgetAllocator.Api.Models.Config;
using BudgetAllocator.Api.Models.DTO.Request;
using BudgetAllocator.Api.Models.DTO.Response;
using BudgetAllocator.Api.Models.DTO.Response.Generic;
using BudgetAllocator.Api.Models.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BudgetAllocator.Api.Services;

public class AuthService : IAuthService
{
    private readonly JwtConfig _jwtConfig;
    private readonly AppDbContext _db;

    public AuthService(IOptions<JwtConfig> jwtConfig, AppDbContext db)
    {
        _jwtConfig = jwtConfig.Value;
        _db = db;
    }

    public async Task<BaseResponse<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var authResponse = await IssueTokensAsync(request.Email, request.Role);
        return BaseResponse<AuthResponse>.Ok(authResponse, "Login successful.");
    }

    public async Task<BaseResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var existingToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

        if (existingToken is null || existingToken.IsRevoked || existingToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return BaseResponse<AuthResponse>.Error("Invalid or expired refresh token.");
        }

        // Token Rotation: Immediately revoke the old refresh token so it can never be reused
        existingToken.IsRevoked = true;

        var authResponse = await IssueTokensAsync(existingToken.Email, existingToken.Role);
        return BaseResponse<AuthResponse>.Ok(authResponse, "Token refreshed successfully.");
    }

    public async Task<BaseResponse<bool>> RevokeTokenAsync(RefreshTokenRequest request)
    {
        var existingToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

        if (existingToken is null || existingToken.IsRevoked)
        {
            return BaseResponse<bool>.Error("Refresh token not found or already revoked.", false);
        }

        existingToken.IsRevoked = true;
        await _db.SaveChangesAsync();

        return BaseResponse<bool>.Ok(true, "Refresh token revoked (logged out).");
    }

    private async Task<AuthResponse> IssueTokensAsync(string email, RoleEnum role)
    {
        var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwtConfig.ExpiryMinutes);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Role, role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: _jwtConfig.Issuer,
            audience: _jwtConfig.Audience,
            claims: claims,
            expires: accessTokenExpiresAt,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        var refreshTokenString = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = refreshTokenString,
            Email = email,
            Role = role,
            ExpiresAtUtc = refreshTokenExpiresAt,
            IsRevoked = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.RefreshTokens.Add(refreshTokenEntity);
        await _db.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenString,
            Email = email,
            Role = role,
            ExpiresAtUtc = accessTokenExpiresAt,
            RefreshTokenExpiresAtUtc = refreshTokenExpiresAt
        };
    }
}