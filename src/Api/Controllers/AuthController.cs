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
    public async Task<IActionResult> Register([FromBody] RegisterRequest model)
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
    public async Task<IActionResult> Login([FromBody] LoginRequest model)
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
        return Ok(new { access_token = accessToken, refresh_token = refreshToken, expires_in = 60 * int.Parse(HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:AccessTokenMinutes"]) });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest model)
    {
        var user = await userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);
        var rotated = await tokenService.RotateRefreshTokenAsync(model.RefreshToken, user, roles);
        if (rotated == null)
        {
            return Unauthorized();
        }

        return Ok(new { access_token = rotated.Value.accessToken, refresh_token = rotated.Value.refreshToken });
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RefreshRequest model)
    {
        var user = await userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return NotFound();
        }

        await tokenService.RevokeRefreshTokenAsync(model.RefreshToken, user);
        return Ok();
    }
}
