using Driventa.Application.DTOs.Common;
using Driventa.Application.DTOs.Roles;
using Driventa.Domain.Entities;
using Driventa.Infrastructure.Identity;
using Driventa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Driventa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "roles.manage")]
public class RolesController : ControllerBase
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _context;

    public RolesController(RoleManager<IdentityRole<Guid>> roleManager, UserManager<ApplicationUser> userManager, AppDbContext context)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RoleResponse>>>> GetAll()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        var roleIds = roles.Select(r => r.Id).ToList();

        var rolePermissions = await _context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .ToListAsync();

        var permissionIds = rolePermissions.Select(rp => rp.PermissionId).Distinct().ToList();
        var permissions = await _context.Permissions
            .Where(p => permissionIds.Contains(p.Id))
            .ToListAsync();
        var permissionLookup = permissions.ToDictionary(p => p.Id);

        var response = roles.Select(r => new RoleResponse
        {
            Id = r.Id,
            Name = r.Name!,
            Permissions = rolePermissions
                .Where(rp => rp.RoleId == r.Id)
                .Select(rp => permissionLookup.TryGetValue(rp.PermissionId, out var p)
                    ? new PermissionResponse { Id = p.Id, Name = p.Name, Description = p.Description }
                    : null)
                .Where(p => p != null)
                .ToList()!
        }).ToList();

        return Ok(ApiResponse<List<RoleResponse>>.Ok(response));
    }

    [HttpGet("permissions")]
    public async Task<ActionResult<ApiResponse<List<PermissionResponse>>>> GetPermissions()
    {
        var permissions = await _context.Permissions
            .OrderBy(p => p.Name)
            .Select(p => new PermissionResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description
            })
            .ToListAsync();

        return Ok(ApiResponse<List<PermissionResponse>>.Ok(permissions));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> GetById(Guid id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
            return NotFound(ApiResponse<RoleResponse>.Fail("Role not found."));

        var rolePermissionIds = await _context.RolePermissions
            .Where(rp => rp.RoleId == id)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        var permissions = await _context.Permissions
            .Where(p => rolePermissionIds.Contains(p.Id))
            .Select(p => new PermissionResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description
            })
            .ToListAsync();

        var response = new RoleResponse
        {
            Id = role.Id,
            Name = role.Name!,
            Permissions = permissions
        };

        return Ok(ApiResponse<RoleResponse>.Ok(response));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> Create([FromBody] CreateRoleRequest request)
    {
        if (await _roleManager.RoleExistsAsync(request.Name))
            return BadRequest(ApiResponse<RoleResponse>.Fail("Role name already exists."));

        var role = new IdentityRole<Guid>
        {
            Name = request.Name,
            NormalizedName = request.Name.ToUpper(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(ApiResponse<RoleResponse>.Fail("Failed to create role.", errors));
        }

        var response = new RoleResponse
        {
            Id = role.Id,
            Name = role.Name!,
            Permissions = new List<PermissionResponse>()
        };

        return Ok(ApiResponse<RoleResponse>.Ok(response, "Role created successfully."));
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> Update(
        Guid id,
        [FromBody] UpdateRoleRequest request)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
            return NotFound(ApiResponse<RoleResponse>.Fail("Role not found."));

        if (await _roleManager.RoleExistsAsync(request.Name) && !role.Name!.Equals(request.Name, StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<RoleResponse>.Fail("Role name already exists."));

        role.Name = request.Name;
        role.NormalizedName = request.Name.ToUpper();

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(ApiResponse<RoleResponse>.Fail("Failed to update role.", errors));
        }

        var rolePermissionIds = await _context.RolePermissions
            .Where(rp => rp.RoleId == id)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        var permissions = await _context.Permissions
            .Where(p => rolePermissionIds.Contains(p.Id))
            .Select(p => new PermissionResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description
            })
            .ToListAsync();

        var response = new RoleResponse
        {
            Id = role.Id,
            Name = role.Name!,
            Permissions = permissions
        };

        return Ok(ApiResponse<RoleResponse>.Ok(response, "Role updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
            return NotFound(ApiResponse<object>.Fail("Role not found."));

        if (role.Name == "SuperAdmin")
            return BadRequest(ApiResponse<object>.Fail("Cannot delete the SuperAdmin role."));

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
        if (usersInRole.Any())
            return Conflict(ApiResponse<object>.Fail("Cannot delete a role that is assigned to users. Reassign users first."));

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(ApiResponse<object>.Fail("Failed to delete role.", errors));
        }

        return Ok(ApiResponse<object>.Ok(new object(), "Role deleted successfully."));
    }

    [HttpPut("{id:guid}/permissions")]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> UpdatePermissions(
        Guid id,
        [FromBody] UpdateRolePermissionsRequest request)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
            return NotFound(ApiResponse<RoleResponse>.Fail("Role not found."));

        // Remove existing permissions
        var existingPermissions = await _context.RolePermissions
            .Where(rp => rp.RoleId == id)
            .ToListAsync();
        _context.RolePermissions.RemoveRange(existingPermissions);

        // Add new permissions
        var newPermissions = request.PermissionIds.Select(pid => new RolePermission
        {
            RoleId = id,
            PermissionId = pid
        }).ToList();
        _context.RolePermissions.AddRange(newPermissions);

        await _context.SaveChangesAsync();

        var permissions = await _context.Permissions
            .Where(p => request.PermissionIds.Contains(p.Id))
            .Select(p => new PermissionResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description
            })
            .ToListAsync();

        var response = new RoleResponse
        {
            Id = role.Id,
            Name = role.Name!,
            Permissions = permissions
        };

        return Ok(ApiResponse<RoleResponse>.Ok(response, "Role permissions updated successfully."));
    }
}
