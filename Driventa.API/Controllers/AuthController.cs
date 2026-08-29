using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Driventa.Application.DTOs.Auth;
using Driventa.Application.DTOs.Common;
using Driventa.Application.Interfaces;
using Driventa.Infrastructure;
using Driventa.Infrastructure.Identity;
using Driventa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Driventa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtSettings _jwtSettings;
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtSettings jwtSettings,
        AppDbContext context,
        IEmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtSettings = jwtSettings;
        _context = context;
        _emailService = emailService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid email or password."));

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
                return Unauthorized(ApiResponse<LoginResponse>.Fail("Account is locked out."));
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid email or password."));
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var token = await GenerateJwtTokenAsync(user, roles);
        var refreshToken = await GenerateRefreshTokenAsync(user);

        return Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse
        {
            AccessToken = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            UserProfile = new UserProfile
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber,
                Role = roles.FirstOrDefault() ?? "User"
            }
        }));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && !rt.IsRevoked && rt.ExpiresAt > DateTimeOffset.UtcNow);

        if (refreshToken == null)
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid refresh token."));

        if (refreshToken.ExpiresAt < DateTimeOffset.UtcNow)
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Refresh token has expired."));

        var user = await _userManager.FindByIdAsync(refreshToken.UserId.ToString());
        if (user == null || !user.IsActive)
            return Unauthorized(ApiResponse<LoginResponse>.Fail("User not found."));

        // Revoke old refresh token
        refreshToken.IsRevoked = true;
        await _context.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(user);
        var newToken = await GenerateJwtTokenAsync(user, roles);
        var newRefreshToken = await GenerateRefreshTokenAsync(user);

        return Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse
        {
            AccessToken = newToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            UserProfile = new UserProfile
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber,
                Role = roles.FirstOrDefault() ?? "User"
            }
        }));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            var refreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == Guid.Parse(userId) && !rt.IsRevoked && rt.ExpiresAt > DateTimeOffset.UtcNow)
                .ToListAsync();

            foreach (var token in refreshTokens)
            {
                token.IsRevoked = true;
            }
            await _context.SaveChangesAsync();
        }

        return Ok();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserProfile>>> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(ApiResponse<UserProfile>.Ok(new UserProfile
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber,
            Role = roles.FirstOrDefault() ?? "User"
        }));
    }

    private async Task<string> GenerateJwtTokenAsync(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add permission claims
        var userRoles = await _userManager.GetRolesAsync(user);
        foreach (var roleName in userRoles)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role != null)
            {
                var permissionIds = await _context.RolePermissions
                    .Where(rp => rp.RoleId == role.Id)
                    .Select(rp => rp.PermissionId)
                    .ToListAsync();

                var permissions = await _context.Permissions
                    .Where(p => permissionIds.Contains(p.Id))
                    .Select(p => p.Name)
                    .ToListAsync();

                foreach (var permission in permissions)
                {
                    claims.Add(new Claim("permission", permission));
                }
            }
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return Ok(ApiResponse<object>.Ok(new object(), "If the email exists, a reset link has been sent."));

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetLink = $"{Request.Scheme}://{Request.Host}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email!)}";

        // TODO: Send email via IEmailService
        await _emailService.SendPasswordResetEmailAsync(user.Email!, resetLink);

        return Ok(ApiResponse<object>.Ok(new object(), "If the email exists, a reset link has been sent."));
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmPassword)
            return BadRequest(ApiResponse<object>.Fail("Passwords do not match."));

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return BadRequest(ApiResponse<object>.Fail("Invalid request."));

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(ApiResponse<object>.Fail("Password reset failed.", errors));
        }

        return Ok(ApiResponse<object>.Ok(new object(), "Password has been reset successfully."));
    }

    private async Task<string> GenerateRefreshTokenAsync(ApplicationUser user)
    {
        var refreshToken = new RefreshToken
        {
            Token = GenerateRandomToken(),
            UserId = user.Id,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            IsRevoked = false
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken.Token;
    }

    private static string GenerateRandomToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}