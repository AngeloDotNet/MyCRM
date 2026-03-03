using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Api.Data;
using Api.Entities;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Api.Services;

public class TokenService(IConfiguration config, ApplicationDbContext db, IHttpContextAccessor httpContextAccessor) : ITokenService
{
    public async Task<(string accessToken, string refreshToken)> GenerateTokensAsync(ApplicationUser user, IEnumerable<string> roles)
    {
        var accessToken = CreateAccessToken(user, roles);
        var refreshToken = CreateRefreshToken();

        await StoreRefreshTokenAsync(refreshToken, user.Id);

        return (accessToken, refreshToken);
    }

    // Rotate. Detect reuse: if presented token is already revoked => revoke all tokens for user (security incident)
    public async Task<(string accessToken, string refreshToken)?> RotateRefreshTokenAsync(string providedRefreshToken, ApplicationUser user, IEnumerable<string> roles)
    {
        var hash = Hash(providedRefreshToken);

        var existing = await db.RefreshTokens
            .Where(x => x.UserId == user.Id && x.TokenHash == hash)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (existing == null)
        {
            // token not found
            return null;
        }

        if (existing.Revoked)
        {
            // refresh token reuse detected -> revoke all tokens for user
            await RevokeAllRefreshTokensAsync(user.Id);
            return null;
        }

        if (existing.ExpiresAt < DateTime.UtcNow)
        {
            // expired
            return null;
        }

        // produce new tokens
        var newAccessToken = CreateAccessToken(user, roles);
        var newRefreshToken = CreateRefreshToken();
        var newHash = Hash(newRefreshToken);

        // mark existing as revoked, set replaced by
        existing.Revoked = true;
        existing.RevokedAt = DateTime.UtcNow;
        existing.ReplacedByHash = newHash;

        // create new refresh token record (store IP/UA for audit)
        var rt = new RefreshToken
        {
            TokenHash = newHash,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(config["Jwt:RefreshTokenDays"] ?? "30")),
            CreatedAt = DateTime.UtcNow,
            Revoked = false,
            IpAddress = GetCurrentIp(),
            UserAgent = GetCurrentUserAgent()
        };

        db.RefreshTokens.Add(rt);
        await db.SaveChangesAsync();

        return (newAccessToken, newRefreshToken);
    }

    public async Task<bool> ValidateRefreshTokenAsync(string refreshToken, ApplicationUser user)
    {
        var hash = Hash(refreshToken);
        var rt = await db.RefreshTokens.Where(x => x.UserId == user.Id && !x.Revoked)
            .OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync();

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
        rt.RevokedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    public async Task RevokeAllRefreshTokensAsync(string userId)
    {
        var tokens = await db.RefreshTokens.Where(x => x.UserId == userId && !x.Revoked).ToListAsync();

        foreach (var t in tokens)
        {
            t.Revoked = true;
            t.RevokedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }

    private string CreateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var jwtSection = config.GetSection("Jwt");
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
        var jwtSection = config.GetSection("Jwt");
        var hash = Hash(refreshToken);

        var rt = new RefreshToken
        {
            TokenHash = hash,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(jwtSection["RefreshTokenDays"] ?? "30")),
            CreatedAt = DateTime.UtcNow,
            Revoked = false,
            IpAddress = GetCurrentIp(),
            UserAgent = GetCurrentUserAgent()
        };

        db.RefreshTokens.Add(rt);
        await db.SaveChangesAsync();
    }

    private static string CreateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    private static string Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));

        return Convert.ToBase64String(bytes);
    }

    private string? GetCurrentIp()
    {
        try
        {
            var ctx = httpContextAccessor.HttpContext;
            if (ctx == null)
            {
                return null;
            }

            // X-Forwarded-For or remote IP
            if (ctx.Request.Headers.TryGetValue("X-Forwarded-For", out var ff))
            {
                return ff.ToString().Split(',')[0].Trim();
            }

            return ctx.Connection.RemoteIpAddress?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private string? GetCurrentUserAgent()
    {
        try
        {
            var ctx = httpContextAccessor.HttpContext;
            return ctx?.Request?.Headers.UserAgent.ToString();
        }
        catch
        {
            return null;
        }
    }
}