using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Api.Controllers;
using Api.DTOs;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IntegrationTests;

public class AuthControllerTests
{
    [Fact]
    public async Task Refresh_ReturnsUnauthorized_WhenRotateReturnsNull()
    {
        var userMgr = new Mock<UserManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
        var signIn = new Mock<SignInManager<ApplicationUser>>(userMgr.Object, Mock.Of<IHttpContextAccessor>(), Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(), null, null, null, null);
        var tokenSvc = new Mock<ITokenService>();
        var user = new ApplicationUser { Id = "uid", Email = "x@t.test", UserName = "x@t.test" };

        userMgr.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        userMgr.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { "User" });
        tokenSvc.Setup(x => x.RotateRefreshTokenAsync(It.IsAny<string>(), It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync((ValueTuple<string, string>?)null);

        var controller = new AuthController(userMgr.Object, tokenSvc.Object, signIn.Object);

        var res = await controller.Refresh(new RefreshRequest(user.Email!, "rtoken"));
        Assert.IsType<UnauthorizedResult>(res);
    }

    [Fact]
    public async Task RevokeAll_CallsTokenService()
    {
        var userMgr = new Mock<UserManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
        var signIn = new Mock<SignInManager<ApplicationUser>>(userMgr.Object, Mock.Of<IHttpContextAccessor>(), Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(), null, null, null, null);
        var tokenSvc = new Mock<ITokenService>();

        var controller = new AuthController(userMgr.Object, tokenSvc.Object, signIn.Object);

        // set user identity with sub claim
        var userId = "testuser";
        var ctx = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Sub, userId) }, "Test"))
        };

        controller.ControllerContext = new ControllerContext { HttpContext = ctx };

        var actionResult = await controller.RevokeAll();
        tokenSvc.Verify(x => x.RevokeAllRefreshTokensAsync(userId), Times.Once);
        Assert.IsType<OkResult>(actionResult);
    }
}
