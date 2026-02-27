using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Api.Data;
using Api.Entities;
using Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Api.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public TokenService(IConfiguration config, UserManager<ApplicationUser> userManager, ApplicationDbContext db)
    {
        _config = config;
        _userManager = userManager;
        _db = db;
    }

    public async Task<(string accessToken, string refreshToken)> GenerateTokensAsync(ApplicationUser user, IEnumerable<string> roles)
    {
        var accessToken = CreateAccessToken(user, roles);
        var refreshToken = CreateRefreshToken();

        await StoreRefreshTokenAsync(refreshToken, user.Id);

        return (accessToken, refreshToken);
    }

    // Atomically rotate: validate provided refresh token, revoke it and create a new one
    public async Task<(string accessToken, string refreshToken)?> RotateRefreshTokenAsync(string providedRefreshToken, ApplicationUser user, IEnumerable<string> roles)
    {
        var hash = Hash(providedRefreshToken);

        var existing = await _db.RefreshTokens
            .Where(x => x.UserId == user.Id && x.TokenHash == hash)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (existing == null || existing.Revoked || existing.ExpiresAt < DateTime.UtcNow)
        {
            // possible reuse or invalid token
            // optional: revoke all tokens for user on detection
            return null;
        }

        // Generate new tokens
        var newAccessToken = CreateAccessToken(user, roles);
        var newRefreshToken = CreateRefreshToken();
        var newHash = Hash(newRefreshToken);

        // Mark existing as revoked and point to replacement
        existing.Revoked = true;
        existing.ReplacedByHash = newHash;
        existing.ExpiresAt = existing.ExpiresAt; // keep for audit

        // Create new refresh token entity
        var rt = new RefreshToken
        {
            TokenHash = newHash,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(_config["Jwt:RefreshTokenDays"] ?? "30")),
            CreatedAt = DateTime.UtcNow,
            Revoked = false
        };

        _db.RefreshTokens.Add(rt);
        await _db.SaveChangesAsync();

        return (newAccessToken, newRefreshToken);
    }

    public async Task<bool> ValidateRefreshTokenAsync(string refreshToken, ApplicationUser user)
    {
        var hash = Hash(refreshToken);
        var rt = await _db.RefreshTokens
            .Where(x => x.UserId == user.Id && !x.Revoked)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
        if (rt == null)
        {
            return false;
        }

        if (rt.TokenHash != hash)
        {
            return false;
        }

        if (rt.ExpiresAt < DateTime.UtcNow)
        {
            return false;
        }

        return true;
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, ApplicationUser user)
    {
        var hash = Hash(refreshToken);
        var rt = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.UserId == user.Id && x.TokenHash == hash && !x.Revoked);
        if (rt == null)
        {
            return;
        }

        rt.Revoked = true;
        await _db.SaveChangesAsync();
    }

    private string CreateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var jwtSection = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim("username", user.UserName ?? "")
            };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSection["AccessTokenMinutes"] ?? "15")),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task StoreRefreshTokenAsync(string refreshToken, string userId)
    {
        var jwtSection = _config.GetSection("Jwt");
        var hash = Hash(refreshToken);
        var rt = new RefreshToken
        {
            TokenHash = hash,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(jwtSection["RefreshTokenDays"] ?? "30")),
            CreatedAt = DateTime.UtcNow,
            Revoked = false
        };
        _db.RefreshTokens.Add(rt);
        await _db.SaveChangesAsync();
    }

    private static string CreateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    private static string Hash(string input)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}