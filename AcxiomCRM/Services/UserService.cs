using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Authorization;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Models.ViewModels;

namespace AcxiomCRM.Services;

public interface IUserService
{
    Task<PagedResult<UserListItemViewModel>> GetUsersAsync(string? searchTerm = null, string? roleFilter = null, bool? activeFilter = null, int page = 1, int pageSize = 10);
    Task<ApplicationUser?> GetUserByIdAsync(string userId);
    Task<string?> GetUserRoleAsync(ApplicationUser user);
    Task<(bool Success, string Message)> CreateUserAsync(UserCreateViewModel model, string currentUserId, string currentUserName);
    Task<(bool Success, string Message)> UpdateUserAsync(UserEditViewModel model, string currentUserId, string currentUserName);
    Task<(bool Success, string Message)> SetActiveStatusAsync(string userId, bool isActive, string currentUserId, string currentUserName);
    Task<(bool Success, string Message)> UnlockAccountAsync(string userId, string currentUserId, string currentUserName);
    Task<(bool Success, string Message)> ResetPasswordAsync(string userId, string newPassword, string currentUserId, string currentUserName);
    Task<List<ApplicationUser>> GetActiveUsersAsync();
    Task<List<ApplicationUser>> GetManagersAsync();
}

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context,
        IAuditService auditService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _auditService = auditService;
    }

    public async Task<PagedResult<UserListItemViewModel>> GetUsersAsync(
        string? searchTerm = null,
        string? roleFilter = null,
        bool? activeFilter = null,
        int page = 1,
        int pageSize = 10)
    {
        var query = _context.Users.AsNoTracking().Include(u => u.Manager).AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(u => u.FullName.Contains(term) || (u.Email != null && u.Email.Contains(term)));
        }

        if (activeFilter.HasValue)
        {
            query = query.Where(u => u.IsActive == activeFilter.Value);
        }

        var total = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var listItems = new List<UserListItemViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault() ?? "None";

            if (!string.IsNullOrWhiteSpace(roleFilter) && !roleName.Equals(roleFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

            listItems.Add(new UserListItemViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = roleName,
                ManagerName = user.Manager?.FullName,
                IsActive = user.IsActive,
                IsLockedOut = isLocked,
                LockoutEnd = user.LockoutEnd?.UtcDateTime,
                CreatedDate = user.CreatedDate,
                LastLoginDate = user.LastLoginDate
            });
        }

        return new PagedResult<UserListItemViewModel>
        {
            Items = listItems,
            TotalCount = total,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
    {
        return await _context.Users.Include(u => u.Manager).FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<string?> GetUserRoleAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return roles.FirstOrDefault();
    }

    public async Task<(bool Success, string Message)> CreateUserAsync(
        UserCreateViewModel model,
        string currentUserId,
        string currentUserName)
    {
        var existing = await _userManager.FindByEmailAsync(model.Email);
        if (existing != null)
            return (false, "A user with this email address already exists.");

        var validRoles = new[] { RoleConstants.Admin, RoleConstants.Manager, RoleConstants.SalesExecutive };
        if (!validRoles.Contains(model.Role))
            return (false, "Invalid role specified.");

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName.Trim(),
            ManagerId = string.IsNullOrWhiteSpace(model.ManagerId) ? null : model.ManagerId,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            var error = string.Join("; ", result.Errors.Select(e => e.Description));
            return (false, $"User creation failed: {error}");
        }

        await _userManager.AddToRoleAsync(user, model.Role);

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "User Created",
            "User",
            user.Id,
            null,
            $"FullName: {user.FullName}, Email: {user.Email}, Role: {model.Role}",
            "Success");

        return (true, "User created successfully.");
    }

    public async Task<(bool Success, string Message)> UpdateUserAsync(
        UserEditViewModel model,
        string currentUserId,
        string currentUserName)
    {
        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
            return (false, "User not found.");

        var validRoles = new[] { RoleConstants.Admin, RoleConstants.Manager, RoleConstants.SalesExecutive };
        if (!validRoles.Contains(model.Role))
            return (false, "Invalid role specified.");

        // Prevent admin from deactivating self
        if (user.Id == currentUserId && !model.IsActive)
            return (false, "You cannot deactivate your own account.");

        var currentRoles = await _userManager.GetRolesAsync(user);
        var oldRole = currentRoles.FirstOrDefault() ?? "None";

        user.FullName = model.FullName.Trim();
        user.IsActive = model.IsActive;
        user.ManagerId = string.IsNullOrWhiteSpace(model.ManagerId) ? null : model.ManagerId;

        // If email changed
        if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailTaken = await _userManager.FindByEmailAsync(model.Email);
            if (emailTaken != null && emailTaken.Id != user.Id)
                return (false, "Another user already uses this email address.");

            user.Email = model.Email;
            user.UserName = model.Email;
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var err = string.Join("; ", updateResult.Errors.Select(e => e.Description));
            return (false, $"Failed to update user: {err}");
        }

        // Role change
        if (oldRole != model.Role)
        {
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }
            await _userManager.AddToRoleAsync(user, model.Role);

            await _auditService.LogAsync(
                currentUserId,
                currentUserName,
                "Role Changed",
                "User",
                user.Id,
                $"Role: {oldRole}",
                $"Role: {model.Role}",
                "Success");
        }

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "User Updated",
            "User",
            user.Id,
            null,
            $"FullName: {user.FullName}, Active: {user.IsActive}",
            "Success");

        return (true, "User updated successfully.");
    }

    public async Task<(bool Success, string Message)> SetActiveStatusAsync(
        string userId,
        bool isActive,
        string currentUserId,
        string currentUserName)
    {
        if (userId == currentUserId && !isActive)
            return (false, "You cannot deactivate your own account.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, "User not found.");

        user.IsActive = isActive;
        await _userManager.UpdateAsync(user);

        var action = isActive ? "User Activated" : "User Deactivated";
        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            action,
            "User",
            user.Id,
            $"Active: {!isActive}",
            $"Active: {isActive}",
            "Success");

        return (true, $"User {(isActive ? "activated" : "deactivated")} successfully.");
    }

    public async Task<(bool Success, string Message)> UnlockAccountAsync(
        string userId,
        string currentUserId,
        string currentUserName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, "User not found.");

        var result = await _userManager.SetLockoutEndDateAsync(user, null);
        if (!result.Succeeded)
            return (false, "Failed to unlock account.");

        await _userManager.ResetAccessFailedCountAsync(user);

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Account Unlock",
            "Authentication",
            user.Id,
            "Locked",
            "Unlocked",
            "Success");

        return (true, "Account unlocked successfully.");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(
        string userId,
        string newPassword,
        string currentUserId,
        string currentUserName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, "User not found.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            var err = string.Join("; ", result.Errors.Select(e => e.Description));
            return (false, $"Password reset failed: {err}");
        }

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Password Reset Admin",
            "Security",
            user.Id,
            null,
            "Admin triggered password reset",
            "Success");

        return (true, "Password has been successfully reset.");
    }

    public async Task<List<ApplicationUser>> GetActiveUsersAsync()
    {
        return await _context.Users.AsNoTracking().Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
    }

    public async Task<List<ApplicationUser>> GetManagersAsync()
    {
        var managerRole = await _roleManager.FindByNameAsync(RoleConstants.Manager);
        var adminRole = await _roleManager.FindByNameAsync(RoleConstants.Admin);

        var managerUserIds = await _context.UserRoles
            .Where(ur => (managerRole != null && ur.RoleId == managerRole.Id) || (adminRole != null && ur.RoleId == adminRole.Id))
            .Select(ur => ur.UserId)
            .ToListAsync();

        return await _context.Users.AsNoTracking()
            .Where(u => managerUserIds.Contains(u.Id) && u.IsActive)
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }
}

