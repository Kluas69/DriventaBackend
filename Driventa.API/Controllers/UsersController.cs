using System.Security.Claims;
using Driventa.Application.DTOs.Common;
using Driventa.Application.DTOs.Users;
using Driventa.Infrastructure.Identity;
using Driventa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Driventa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "users.view")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly AppDbContext _context;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        AppDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = "users.view")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<UserResponse>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? roleId = null,
        [FromQuery] bool? isActive = null)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u =>
                u.FirstName.Contains(search) ||
                u.LastName.Contains(search) ||
                u.Email!.Contains(search));

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = new List<UserResponse>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault();
            Guid? roleIdValue = null;

            if (!string.IsNullOrEmpty(roleName))
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                roleIdValue = role?.Id;
            }

            // Filter by roleId after fetching (since Identity doesn't support it in query)
            if (roleId.HasValue && roleIdValue != roleId.Value)
                continue;

            response.Add(new UserResponse
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber,
                Role = roleName ?? "User",
                RoleId = roleIdValue,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            });
        }

        return Ok(ApiResponse<PaginatedResponse<UserResponse>>.Ok(
            new PaginatedResponse<UserResponse>
            {
                Items = response,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> GetById(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound(ApiResponse<UserResponse>.Fail("User not found."));

        var roles = await _userManager.GetRolesAsync(user);
        var roleName = roles.FirstOrDefault();
        Guid? roleIdValue = null;

        if (!string.IsNullOrEmpty(roleName))
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            roleIdValue = role?.Id;
        }

        var response = new UserResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber,
            Role = roleName ?? "User",
            RoleId = roleIdValue,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };

        return Ok(ApiResponse<UserResponse>.Ok(response));
    }

    [HttpPost]
    [Authorize(Policy = "users.create")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Create([FromBody] CreateUserRequest request)
    {
        if (request.Password != request.ConfirmPassword)
            return BadRequest(ApiResponse<UserResponse>.Fail("Passwords do not match."));

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            return BadRequest(ApiResponse<UserResponse>.Fail("A user with this email already exists."));

        var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());
        if (role == null)
            return BadRequest(ApiResponse<UserResponse>.Fail("Invalid role."));

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(ApiResponse<UserResponse>.Fail("Failed to create user.", errors));
        }

        await _userManager.AddToRoleAsync(user, role.Name!);

        _context.ActivityLogs.Add(new Domain.Entities.ActivityLog
        {
            Action = "Create",
            EntityType = "User",
            EntityId = user.Id,
            Description = $"User {user.Email} created with role {role.Name}"
        });

        await _context.SaveChangesAsync();

        var response = new UserResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber,
            Role = role.Name!,
            RoleId = role.Id,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };

        return Ok(ApiResponse<UserResponse>.Ok(response, "User created successfully."));
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Update(
        Guid id,
        [FromBody] UpdateUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound(ApiResponse<UserResponse>.Fail("User not found."));

        if (request.FirstName != null) user.FirstName = request.FirstName;
        if (request.LastName != null) user.LastName = request.LastName;
        if (request.PhoneNumber != null) user.PhoneNumber = request.PhoneNumber;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(ApiResponse<UserResponse>.Fail("Failed to update user.", errors));
        }

        _context.ActivityLogs.Add(new Domain.Entities.ActivityLog
        {
            Action = "Update",
            EntityType = "User",
            EntityId = id,
            Description = $"User {user.Email} profile updated"
        });

        await _context.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(user);
        var roleName = roles.FirstOrDefault();
        Guid? roleIdValue = null;

        if (!string.IsNullOrEmpty(roleName))
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            roleIdValue = role?.Id;
        }

        var response = new UserResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber,
            Role = roleName ?? "User",
            RoleId = roleIdValue,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };

        return Ok(ApiResponse<UserResponse>.Ok(response, "User updated successfully."));
    }

    [HttpPut("{id:guid}/role")]
    [Authorize(Policy = "users.edit")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> UpdateRole(
        Guid id,
        [FromBody] UpdateUserRoleRequest request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound(ApiResponse<UserResponse>.Fail("User not found."));

        var newRole = await _roleManager.FindByIdAsync(request.RoleId.ToString());
        if (newRole == null)
            return BadRequest(ApiResponse<UserResponse>.Fail("Invalid role."));

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, newRole.Name!);

        _context.ActivityLogs.Add(new Domain.Entities.ActivityLog
        {
            Action = "RoleChange",
            EntityType = "User",
            EntityId = id,
            Description = $"User {user.Email} role changed to {newRole.Name}"
        });

        await _context.SaveChangesAsync();

        var response = new UserResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber,
            Role = newRole.Name!,
            RoleId = newRole.Id,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };

        return Ok(ApiResponse<UserResponse>.Ok(response, "User role updated successfully."));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "users.delete")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> UpdateStatus(
        Guid id,
        [FromBody] UpdateUserStatusRequest request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound(ApiResponse<UserResponse>.Fail("User not found."));

        // Prevent deactivating the last active SuperAdmin
        if (!request.IsActive)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("SuperAdmin"))
            {
                var superAdminCount = await _userManager.Users
                    .Where(u => u.IsActive)
                    .CountAsync();

                var activeSuperAdmins = 0;
                foreach (var u in await _userManager.Users.Where(u => u.IsActive).ToListAsync())
                {
                    if ((await _userManager.GetRolesAsync(u)).Contains("SuperAdmin"))
                        activeSuperAdmins++;
                }

                if (activeSuperAdmins <= 1)
                    return BadRequest(ApiResponse<UserResponse>.Fail("Cannot deactivate the last active SuperAdmin."));
            }
        }

        user.IsActive = request.IsActive;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(ApiResponse<UserResponse>.Fail("Failed to update user status.", errors));
        }

        _context.ActivityLogs.Add(new Domain.Entities.ActivityLog
        {
            Action = request.IsActive ? "Activate" : "Deactivate",
            EntityType = "User",
            EntityId = id,
            Description = $"User {user.Email} {(request.IsActive ? "activated" : "deactivated")}"
        });

        await _context.SaveChangesAsync();

        var userRoles = await _userManager.GetRolesAsync(user);
        var roleName = userRoles.FirstOrDefault();
        Guid? roleIdValue = null;

        if (!string.IsNullOrEmpty(roleName))
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            roleIdValue = role?.Id;
        }

        var response = new UserResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber,
            Role = roleName ?? "User",
            RoleId = roleIdValue,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };

        return Ok(ApiResponse<UserResponse>.Ok(response, "User status updated successfully."));
    }
}
