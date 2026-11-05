using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email or Username is required.")]
    [Display(Name = "Email or Username")]
    public string EmailOrUsername { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember Me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full Name must be between 2 and 100 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
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
}

public class ResetPasswordViewModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    public string? UserName { get; set; }

    [Required(ErrorMessage = "New Password is required.")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "Password must be at least 10 characters long.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm Password is required.")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

