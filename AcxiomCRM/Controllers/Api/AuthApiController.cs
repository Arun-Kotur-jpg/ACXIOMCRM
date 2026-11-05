using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using AcxiomCRM.Models;
using AcxiomCRM.Models.DTOs;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public AuthApiController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditService = auditService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthRateLimit")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = "Invalid request payload.",
                Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
            });
        }

        var user = await _userManager.FindByEmailAsync(model.EmailOrUsername)
                   ?? await _userManager.FindByNameAsync(model.EmailOrUsername);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (user == null || !user.IsActive)
        {
            await _auditService.LogAsync(user?.Id, model.EmailOrUsername, "API Login Failed", "Authentication", null, null, null, "Failed", ip);
            return Unauthorized(new { message = "Invalid email/username or password, or account is deactivated." });
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, isPersistent: false, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "None";

            await _auditService.LogAsync(user.Id, user.FullName, "API Login Succeeded", "Authentication", user.Id, null, null, "Success", ip);

            return Ok(new LoginResponseDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = role,
                Message = "Authentication successful."
            });
        }

        if (result.IsLockedOut)
        {
            await _auditService.LogAsync(user.Id, user.FullName, "API Account Lockout", "Authentication", user.Id, null, null, "Failed", ip);
            return StatusCode(StatusCodes.Status423Locked, new { message = "Account is locked out due to multiple failed login attempts. Try again in 15 minutes." });
        }

        await _auditService.LogAsync(user.Id, user.FullName, "API Login Failed", "Authentication", user.Id, null, null, "Failed", ip);
        return Unauthorized(new { message = "Invalid email/username or password." });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = _userManager.GetUserId(User);
        var userName = User.Identity?.Name;
        await _signInManager.SignOutAsync();

        if (userId != null)
        {
            await _auditService.LogAsync(userId, userName, "API Logout", "Authentication", userId, null, null, "Success");
        }

        return Ok(new { message = "Logged out successfully." });
    }
}

