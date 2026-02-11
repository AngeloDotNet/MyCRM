using Api.DTOs;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(UserManager<ApplicationUser> userManager, ITokenService tokenService, SignInManager<ApplicationUser> signInManager) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest model)
    {
        var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
        var res = await userManager.CreateAsync(user, model.Password);

        if (!res.Succeeded)
        {
            return BadRequest(res.Errors);
        }

        await userManager.AddToRoleAsync(user, "User");
        return Ok();
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest model)
    {
        var user = await userManager.FindByEmailAsync(model.Email);

        if (user == null)
        {
            return Unauthorized();
        }

        var check = await signInManager.CheckPasswordSignInAsync(user, model.Password, false);

        if (!check.Succeeded)
        {
            return Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, refreshToken) = await tokenService.GenerateTokensAsync(user, roles);

        var AccessTokenMinutes = int.Parse(RequestServices().GetRequiredService<IConfiguration>()["Jwt:AccessTokenMinutes"]!);

        return Ok(new { access_token = accessToken, refresh_token = refreshToken, expires_in = 60 * AccessTokenMinutes });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshAsync([FromBody] RefreshRequest model)
    {
        var principal = HttpContext.User;
        //var email = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email;

        var user = await userManager.FindByEmailAsync(model.Email);

        if (user == null)
        {
            return Unauthorized();
        }

        var valid = await tokenService.ValidateRefreshTokenAsync(model.RefreshToken, user);

        if (!valid)
        {
            return Unauthorized();
        }

        // rotate: revoke old token and issue new pair
        await tokenService.RevokeRefreshTokenAsync(model.RefreshToken, user);

        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, refreshToken) = await tokenService.GenerateTokensAsync(user, roles);

        return Ok(new { access_token = accessToken, refresh_token = refreshToken });
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeAsync([FromBody] RefreshRequest model)
    {
        var user = await userManager.FindByEmailAsync(model.Email);

        if (user == null)
        {
            return NotFound();
        }

        await tokenService.RevokeRefreshTokenAsync(model.RefreshToken, user);
        return Ok();
    }

    private IServiceProvider RequestServices() => HttpContext.RequestServices;
}