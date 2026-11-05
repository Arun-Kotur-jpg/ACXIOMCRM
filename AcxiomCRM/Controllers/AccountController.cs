using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using AcxiomCRM.Authorization;
using AcxiomCRM.Models;
using AcxiomCRM.Models.ViewModels;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;
    private readonly IConfiguration _configuration;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService,
        IConfiguration configuration)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditService = auditService;
        _configuration = configuration;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("AuthRateLimit")]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        // Find user by email or username
        var user = await _userManager.FindByEmailAsync(model.EmailOrUsername)
                   ?? await _userManager.FindByNameAsync(model.EmailOrUsername);

        if (user == null)
        {
            await _auditService.LogAsync(
                null,
                model.EmailOrUsername,
                "Failed Login",
                "Authentication",
                null,
                null,
                null,
                "Failed",
                ip,
                "Unknown user credential attempt");

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        // Inactive account check
        if (!user.IsActive)
        {
            await _auditService.LogAsync(
                user.Id,
                user.FullName,
                "Failed Login - Inactive Account",
                "Authentication",
                user.Id,
                null,
                null,
                "Failed",
                ip,
                "Attempt to login to deactivated account");

            ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact your system administrator.");
            return View(model);
        }

        // Check password sign in with lockout enabled
        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            await _auditService.LogAsync(
                user.Id,
                user.FullName,
                "Successful Login",
                "Authentication",
                user.Id,
                null,
                null,
                "Success",
                ip,
                "User logged in successfully");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            await _auditService.LogAsync(
                user.Id,
                user.FullName,
                "Account Lockout",
                "Authentication",
                user.Id,
                null,
                null,
                "Failed",
                ip,
                "Account locked out due to multiple failed attempts");

            ModelState.AddModelError(string.Empty, "Account locked due to multiple failed login attempts. Please try again after 15 minutes or contact your administrator.");
            return View(model);
        }

        await _auditService.LogAsync(
            user.Id,
            user.FullName,
            "Failed Login",
            "Authentication",
            user.Id,
            null,
            null,
            "Failed",
            ip,
            "Incorrect password");

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        var allowReg = _configuration.GetValue<bool>("AllowPublicRegistration", true);
        if (!allowReg)
        {
            TempData["ErrorMessage"] = "Public registration is currently disabled.";
            return RedirectToAction("Login");
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("AuthRateLimit")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        var allowReg = _configuration.GetValue<bool>("AllowPublicRegistration", true);
        if (!allowReg)
        {
            TempData["ErrorMessage"] = "Public registration is disabled.";
            return RedirectToAction("Login");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existing = await _userManager.FindByEmailAsync(model.Email);
        if (existing != null)
        {
            ModelState.AddModelError(nameof(model.Email), "A user with this email address already exists.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim().ToLowerInvariant(),
            Email = model.Email.Trim().ToLowerInvariant(),
            FullName = model.FullName.Trim(),
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            // Default role is always strictly Sales Executive for public registration
            await _userManager.AddToRoleAsync(user, RoleConstants.SalesExecutive);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _auditService.LogAsync(
                user.Id,
                user.FullName,
                "User Self-Registered",
                "Authentication",
                user.Id,
                null,
                $"Role: {RoleConstants.SalesExecutive}",
                "Success",
                ip,
                "New sales executive self-registered");

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Dashboard");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = _userManager.GetUserId(User);
        var userName = User.Identity?.Name;
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        await _signInManager.SignOutAsync();

        if (userId != null)
        {
            await _auditService.LogAsync(
                userId,
                userName,
                "Logout",
                "Authentication",
                userId,
                null,
                null,
                "Success",
                ip,
                "User signed out");
        }

        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}

