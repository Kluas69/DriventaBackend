using Driventa.Domain.Entities;
using Driventa.Infrastructure.Persistence;
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

    public static readonly (string Name, string? Description)[] Permissions =
    [
        // Users
        ("users.view", "View user profiles"),
        ("users.create", "Create new users"),
        ("users.edit", "Edit user profiles"),
        ("users.delete", "Deactivate or delete users"),

        // Applications
        ("applications.view", "View applications"),
        ("applications.edit", "Edit application details"),
        ("applications.assign", "Assign applications to users"),
        ("applications.convert", "Convert applications to carriers"),

        // Carriers
        ("carriers.view", "View carriers"),
        ("carriers.create", "Create new carriers"),
        ("carriers.edit", "Edit carrier details"),

        // Loads
        ("loads.view", "View loads"),
        ("loads.create", "Create new loads"),
        ("loads.edit", "Edit load details"),
        ("loads.updateStatus", "Update load status"),

        // Billing
        ("billing.view", "View invoices and payments"),
        ("billing.create", "Create invoices and record payments"),
        ("billing.manage", "Manage billing settings and overrides"),

        // Reports
        ("reports.view", "View reports and analytics"),

        // Roles
        ("roles.manage", "Manage roles and permissions"),
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

    public static async Task SeedPermissionsAsync(AppDbContext context)
    {
        if (await context.Permissions.AnyAsync())
            return;

        var permissionEntities = Permissions.Select(p => new Permission
        {
            Id = Guid.NewGuid(),
            Name = p.Name,
            Description = p.Description
        }).ToList();

        context.Permissions.AddRange(permissionEntities);
        await context.SaveChangesAsync();

        // Assign permissions to roles
        var allPermissions = await context.Permissions.ToListAsync();
        var permissionLookup = allPermissions.ToDictionary(p => p.Name);

        var rolePermissions = new List<RolePermission>();

        // SuperAdmin gets all permissions
        var superAdminRoleId = await context.Roles
            .Where(r => r.Name == "SuperAdmin")
            .Select(r => r.Id)
            .FirstAsync();
        foreach (var perm in allPermissions)
        {
            rolePermissions.Add(new RolePermission { RoleId = superAdminRoleId, PermissionId = perm.Id });
        }

        // Admin gets everything except roles.manage
        var adminRoleId = await context.Roles
            .Where(r => r.Name == "Admin")
            .Select(r => r.Id)
            .FirstAsync();
        foreach (var perm in allPermissions.Where(p => p.Name != "roles.manage"))
        {
            rolePermissions.Add(new RolePermission { RoleId = adminRoleId, PermissionId = perm.Id });
        }

        // DispatchManager
        var dispatchManagerRoleId = await context.Roles
            .Where(r => r.Name == "DispatchManager")
            .Select(r => r.Id)
            .FirstAsync();
        var dispatchManagerPerms = new[]
        {
            "applications.view", "applications.edit", "applications.assign", "applications.convert",
            "carriers.view", "carriers.create", "carriers.edit",
            "loads.view", "loads.create", "loads.edit", "loads.updateStatus",
            "billing.view", "billing.create",
            "reports.view"
        };
        foreach (var permName in dispatchManagerPerms)
        {
            if (permissionLookup.TryGetValue(permName, out var perm))
                rolePermissions.Add(new RolePermission { RoleId = dispatchManagerRoleId, PermissionId = perm.Id });
        }

        // Dispatcher
        var dispatcherRoleId = await context.Roles
            .Where(r => r.Name == "Dispatcher")
            .Select(r => r.Id)
            .FirstAsync();
        var dispatcherPerms = new[]
        {
            "applications.view", "applications.edit",
            "carriers.view",
            "loads.view", "loads.create", "loads.edit", "loads.updateStatus",
            "reports.view"
        };
        foreach (var permName in dispatcherPerms)
        {
            if (permissionLookup.TryGetValue(permName, out var perm))
                rolePermissions.Add(new RolePermission { RoleId = dispatcherRoleId, PermissionId = perm.Id });
        }

        // Sales
        var salesRoleId = await context.Roles
            .Where(r => r.Name == "Sales")
            .Select(r => r.Id)
            .FirstAsync();
        var salesPerms = new[]
        {
            "applications.view", "applications.edit",
            "carriers.view", "carriers.create",
            "loads.view",
            "reports.view"
        };
        foreach (var permName in salesPerms)
        {
            if (permissionLookup.TryGetValue(permName, out var perm))
                rolePermissions.Add(new RolePermission { RoleId = salesRoleId, PermissionId = perm.Id });
        }

        context.RolePermissions.AddRange(rolePermissions);
        await context.SaveChangesAsync();
    }

    public static async Task SeedSuperAdminAsync(UserManager<ApplicationUser> userManager)
    {
        const string superAdminEmail = "admin@driventa.com";
        var existingUser = await userManager.FindByEmailAsync(superAdminEmail);

        if (existingUser == null)
        {
            existingUser = new ApplicationUser
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                EmailConfirmed = true,
                FirstName = "Super",
                LastName = "Admin",
                PhoneNumberConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(existingUser, "Admin@123");
            if (!result.Succeeded)
                return;
        }

        if (!await userManager.IsInRoleAsync(existingUser, "SuperAdmin"))
        {
            await userManager.AddToRoleAsync(existingUser, "SuperAdmin");
        }
    }
}
