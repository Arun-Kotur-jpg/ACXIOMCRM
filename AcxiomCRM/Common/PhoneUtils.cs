using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace AcxiomCRM.Common;

public static class PhoneUtils
{
    // Indian mobile numbers start with 6, 7, 8, or 9 and have 10 digits, optionally preceded by +91 or 91 or 0
    private static readonly Regex IndianPhoneRegex = new(
        @"^(?:\+91|91|0)?[6-9]\d{9}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool IsValidIndianPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        var clean = phone.Trim().Replace(" ", "").Replace("-", "");
        return IndianPhoneRegex.IsMatch(clean);
    }

    public static string NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var clean = phone.Trim().Replace(" ", "").Replace("-", "");
        if (clean.StartsWith("+91"))
            clean = clean.Substring(3);
        else if (clean.StartsWith("91") && clean.Length == 12)
            clean = clean.Substring(2);
        else if (clean.StartsWith("0") && clean.Length == 11)
            clean = clean.Substring(1);

        return clean;
    }

    public static string NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant();
    }
}

public class IndianPhoneAttribute : ValidationAttribute
{
    public IndianPhoneAttribute()
        : base("Enter a valid 10-digit Indian mobile number (e.g., 9876543210 or +919876543210).")
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return ValidationResult.Success; // [Required] handles emptiness if needed
        }

        var str = value.ToString()!;
        if (PhoneUtils.IsValidIndianPhone(str))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(ErrorMessage);
    }
}

