using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Authorization;
using AcxiomCRM.Models.ViewModels;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers;

[Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
public class UsersController : BaseController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchTerm = null,
        string? roleFilter = null,
        bool? activeFilter = null,
        int page = 1)
    {
        var result = await _userService.GetUsersAsync(searchTerm, roleFilter, activeFilter, page, 10);

        var model = new UserListViewModel
        {
            Users = result,
            SearchTerm = searchTerm,
            RoleFilter = roleFilter,
            ActiveFilter = activeFilter
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var managers = await _userService.GetManagersAsync();
        var roles = new[] { RoleConstants.Admin, RoleConstants.Manager, RoleConstants.SalesExecutive };

        var model = new UserCreateViewModel
        {
            Role = RoleConstants.SalesExecutive,
            AvailableRoles = new SelectList(roles),
            AvailableManagers = new SelectList(managers, "Id", "FullName")
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var managers = await _userService.GetManagersAsync();
            var roles = new[] { RoleConstants.Admin, RoleConstants.Manager, RoleConstants.SalesExecutive };
            model.AvailableRoles = new SelectList(roles, model.Role);
            model.AvailableManagers = new SelectList(managers, "Id", "FullName", model.ManagerId);
            return View(model);
        }

        var (success, message) = await _userService.CreateUserAsync(
            model,
            CurrentUserId,
            CurrentUserName);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            var managers = await _userService.GetManagersAsync();
            var roles = new[] { RoleConstants.Admin, RoleConstants.Manager, RoleConstants.SalesExecutive };
            model.AvailableRoles = new SelectList(roles, model.Role);
            model.AvailableManagers = new SelectList(managers, "Id", "FullName", model.ManagerId);
            return View(model);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var role = await _userService.GetUserRoleAsync(user);
        var managers = await _userService.GetManagersAsync();
        var roles = new[] { RoleConstants.Admin, RoleConstants.Manager, RoleConstants.SalesExecutive };

        var model = new UserEditViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = role ?? RoleConstants.SalesExecutive,
            ManagerId = user.ManagerId,
            IsActive = user.IsActive,
            AvailableRoles = new SelectList(roles, role),
            AvailableManagers = new SelectList(managers.Where(m => m.Id != user.Id), "Id", "FullName", user.ManagerId)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UserEditViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            var managers = await _userService.GetManagersAsync();
            var roles = new[] { RoleConstants.Admin, RoleConstants.Manager, RoleConstants.SalesExecutive };
            model.AvailableRoles = new SelectList(roles, model.Role);
            model.AvailableManagers = new SelectList(managers.Where(m => m.Id != model.Id), "Id", "FullName", model.ManagerId);
            return View(model);
        }

        var (success, message) = await _userService.UpdateUserAsync(
            model,
            CurrentUserId,
            CurrentUserName);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            var managers = await _userService.GetManagersAsync();
            var roles = new[] { RoleConstants.Admin, RoleConstants.Manager, RoleConstants.SalesExecutive };
            model.AvailableRoles = new SelectList(roles, model.Role);
            model.AvailableManagers = new SelectList(managers.Where(m => m.Id != model.Id), "Id", "FullName", model.ManagerId);
            return View(model);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id, bool isActive)
    {
        var (success, message) = await _userService.SetActiveStatusAsync(
            id,
            isActive,
            CurrentUserId,
            CurrentUserName);

        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id)
    {
        var (success, message) = await _userService.UnlockAccountAsync(
            id,
            CurrentUserId,
            CurrentUserName);

        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 10)
        {
            TempData["ErrorMessage"] = "Password must be at least 10 characters long.";
            return RedirectToAction(nameof(Index));
        }

        var (success, message) = await _userService.ResetPasswordAsync(
            id,
            newPassword,
            CurrentUserId,
            CurrentUserName);

        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

        return RedirectToAction(nameof(Index));
    }
}

