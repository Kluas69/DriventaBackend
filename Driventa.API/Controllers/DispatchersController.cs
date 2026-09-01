using Driventa.Application.DTOs.Common;
using Driventa.Application.DTOs.Dispatchers;
using Driventa.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Driventa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DispatchersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public DispatchersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<DispatcherResponse>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var users = await _userManager.GetUsersInRoleAsync("Dispatcher");
        var query = users
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.FirstName.Contains(term) ||
                u.LastName.Contains(term) ||
                (u.Email != null && u.Email.Contains(term)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));
        }

        var totalCount = query.Count();
        var items = query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new DispatcherResponse
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email ?? string.Empty,
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToList();

        return Ok(ApiResponse<PaginatedResponse<DispatcherResponse>>.Ok(
            new PaginatedResponse<DispatcherResponse>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }));
    }

    [HttpPost]
    [Authorize(Policy = "users.create")]
    public async Task<ActionResult<ApiResponse<DispatcherResponse>>> Create(
        [FromBody] CreateDispatcherRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(ApiResponse<DispatcherResponse>.Fail("First name, last name, email, and password are required."));
        }

        if (request.Password != request.ConfirmPassword)
        {
            return BadRequest(ApiResponse<DispatcherResponse>.Fail("Passwords do not match."));
        }

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest(ApiResponse<DispatcherResponse>.Fail("A user with this email already exists."));
        }

        var dispatcherRole = await _roleManager.FindByNameAsync("Dispatcher");
        if (dispatcherRole == null)
        {
            return BadRequest(ApiResponse<DispatcherResponse>.Fail("Dispatcher role does not exist."));
        }

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
            return BadRequest(ApiResponse<DispatcherResponse>.Fail("Failed to create dispatcher.", errors));
        }

        await _userManager.AddToRoleAsync(user, dispatcherRole.Name!);

        var response = new DispatcherResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };

        return Ok(ApiResponse<DispatcherResponse>.Ok(response, "Dispatcher created successfully."));
    }
}
