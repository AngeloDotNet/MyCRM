using Api.Enums;
using Api.Models;
using Microsoft.AspNetCore.Identity;

namespace Api.Data;

public static class SeedData
{
    public static async Task EnsureSeedDataAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var scoped = scope.ServiceProvider;

        var userManager = scoped.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scoped.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync(nameof(RolesEnums.Admin)))
        {
            await roleManager.CreateAsync(new IdentityRole(nameof(RolesEnums.Admin)));
        }

        if (!await roleManager.RoleExistsAsync(nameof(RolesEnums.User)))
        {
            await roleManager.CreateAsync(new IdentityRole(nameof(RolesEnums.User)));
        }

        var adminUsername = configuration["AdminUser:Username"] ?? "admin";
        var adminPassword = configuration["AdminUser:Password"] ?? "Admin@12345";
        var adminEmail = configuration["AdminUser:Email"] ?? "admin@local";

        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin == null)
        {
            admin = new ApplicationUser { Email = adminEmail, UserName = adminUsername, EmailConfirmed = true };

            await userManager.CreateAsync(admin, adminPassword);
            await userManager.AddToRoleAsync(admin, nameof(RolesEnums.Admin));
        }
    }
}