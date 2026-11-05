using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Models.ViewModels;

public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ManagerName { get; set; }
    public bool IsActive { get; set; }
    public bool IsLockedOut { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
}

public class UserListViewModel
{
    public PagedResult<UserListItemViewModel> Users { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? RoleFilter { get; set; }
    public bool? ActiveFilter { get; set; }
}

public class UserCreateViewModel
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full Name must be between 2 and 100 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "Password must be at least 10 characters long.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = string.Empty;

    [Display(Name = "Reporting Manager (Optional)")]
    public string? ManagerId { get; set; }

    public SelectList? AvailableRoles { get; set; }
    public SelectList? AvailableManagers { get; set; }
}

public class UserEditViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full Name must be between 2 and 100 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = string.Empty;

    [Display(Name = "Reporting Manager (Optional)")]
    public string? ManagerId { get; set; }

    [Display(Name = "Account Active")]
    public bool IsActive { get; set; } = true;

    public SelectList? AvailableRoles { get; set; }
    public SelectList? AvailableManagers { get; set; }
}

