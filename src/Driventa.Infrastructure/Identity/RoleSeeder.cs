using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Driventa.Infrastructure.Identity;

public static class RoleSeeder
{
    public static readonly string[] Roles =
    [
        "SuperAdmin",
        "Admin",
        "DispatchManager",
        "Dispatcher",
        "Sales"
    ];

    public static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>
                {
                    Name = role,
                    NormalizedName = role.ToUpper(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                });
            }
        }
    }

    public static async Task SeedSuperAdminAsync(UserManager<ApplicationUser> userManager)
    {
        const string superAdminEmail = "admin@driventa.com";
        var existingUser = await userManager.FindByEmailAsync(superAdminEmail);

        if (existingUser == null)
        {
            var superAdmin = new ApplicationUser
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                EmailConfirmed = true,
                FirstName = "Super",
                LastName = "Admin",
                PhoneNumberConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(superAdmin, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
            }
        }
    }
}