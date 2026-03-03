using System.Net;
using Api.Data;
using Api.Entities;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace IntegrationTests;

public class TokenServiceTests
{
    private IConfiguration CreateConfiguration()
    {
        var dict = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "ThisIsA_ChangeMe_ReplaceWithStrongKey12345!",
            ["Jwt:Issuer"] = "MyCrmApi",
            ["Jwt:Audience"] = "MyCrmClients",
            ["Jwt:AccessTokenMinutes"] = "15",
            ["Jwt:RefreshTokenDays"] = "30"
        };
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private (ApplicationDbContext db, TokenService svc) CreateService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"testdb_{Guid.NewGuid()}")
            .Options;

        var db = new ApplicationDbContext(options);

        //var userStore = new Mock<IUserStore<ApplicationUser>>().Object;
        //var userManager = new Mock<UserManager<ApplicationUser>>(userStore, null, null, null, null, null, null, null, null).Object;

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        };

        httpContextAccessor.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        httpContextAccessor.HttpContext.Request.Headers.UserAgent = "UnitTestAgent/1.0";

        //var svc = new TokenService(CreateConfiguration(), userManager, db, httpContextAccessor);
        var svc = new TokenService(CreateConfiguration(), db, httpContextAccessor);
        return (db, svc);
    }

    [Fact]
    public async Task GenerateTokensAsync_StoresRefreshToken_WithIpAndUserAgent()
    {
        var (db, svc) = CreateService();
        var user = new ApplicationUser { Id = "user1", Email = "u@t.test", UserName = "u@t.test" };
        // ensure DB user not required for token service
        db.Add(user);
        await db.SaveChangesAsync();

        var result = await svc.GenerateTokensAsync(user, ["User"]);

        Assert.False(string.IsNullOrEmpty(result.accessToken));
        Assert.False(string.IsNullOrEmpty(result.refreshToken));

        var stored = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.UserId == user.Id);
        Assert.NotNull(stored);
        Assert.Equal("127.0.0.1", stored.IpAddress);
        Assert.Contains("UnitTestAgent", stored.UserAgent);
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_WhenTokenRevoked_RevokeAllAndReturnNull()
    {
        var (db, svc) = CreateService();
        var user = new ApplicationUser { Id = "user2", Email = "u2@t.test", UserName = "u2@t.test" };
        db.Add(user);

        var token = "oldtoken";
        var hashed = typeof(TokenService)
            .GetMethod("Hash", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, [token]) as string;

        db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = hashed!,
            UserId = user.Id,
            CreatedAt = System.DateTime.UtcNow.AddMinutes(-10),
            ExpiresAt = System.DateTime.UtcNow.AddDays(1),
            Revoked = true, // simulate reuse scenario
            IpAddress = "127.0.0.1",
            UserAgent = "UA"
        });

        await db.SaveChangesAsync();

        var rotated = await svc.RotateRefreshTokenAsync(token, user, ["User"]);

        Assert.Null(rotated);

        var all = await db.RefreshTokens.ToListAsync();
        Assert.All(all, rt => Assert.True(rt.Revoked));
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_Success_RotatesToken()
    {
        var (db, svc) = CreateService();
        var user = new ApplicationUser { Id = "user3", Email = "u3@t.test", UserName = "u3@t.test" };
        db.Add(user);

        var token = "validtoken";
        var hashed = typeof(TokenService)
            .GetMethod("Hash", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, [token]) as string;

        db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = hashed!,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            Revoked = false,
            IpAddress = "127.0.0.1",
            UserAgent = "UA"
        });

        await db.SaveChangesAsync();

        var rotated = await svc.RotateRefreshTokenAsync(token, user, new[] { "User" });
        Assert.NotNull(rotated);
        Assert.False(string.IsNullOrEmpty(rotated.Value.accessToken));
        Assert.False(string.IsNullOrEmpty(rotated.Value.refreshToken));

        var old = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hashed);
        Assert.True(old!.Revoked);

        var newTokenHash = typeof(TokenService)
            .GetMethod("Hash", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, [rotated.Value.refreshToken]) as string;

        var existsNew = await db.RefreshTokens.AnyAsync(rt => rt.TokenHash == newTokenHash);
        Assert.True(existsNew);
    }
}