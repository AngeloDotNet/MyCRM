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

public class TokenService(IConfiguration config, UserManager<ApplicationUser> userManager, ApplicationDbContext db) : ITokenService
{
    public async Task<(string accessToken, string refreshToken)> GenerateTokensAsync(ApplicationUser user, IEnumerable<string> roles)
    {
        var jwtSection = config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"] ?? string.Empty));

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

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        // refresh token generation (random GUID)
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshHash = Hash(refreshToken);

        var rt = new RefreshToken
        {
            TokenHash = refreshHash,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(jwtSection["RefreshTokenDays"] ?? "30")),
            CreatedAt = DateTime.UtcNow
        };

        db.RefreshTokens.Add(rt);
        await db.SaveChangesAsync();

        return (accessToken, refreshToken);
    }

    public async Task<bool> ValidateRefreshTokenAsync(string refreshToken, ApplicationUser user)
    {
        var hash = Hash(refreshToken);
        var rt = await db.RefreshTokens.Where(x => x.UserId == user.Id && !x.Revoked).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync();

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
        var rt = await db.RefreshTokens.FirstOrDefaultAsync(x => x.UserId == user.Id && x.TokenHash == hash && !x.Revoked);

        if (rt == null)
        {
            return;
        }

        rt.Revoked = true;
        await db.SaveChangesAsync();
    }

    private static string Hash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}