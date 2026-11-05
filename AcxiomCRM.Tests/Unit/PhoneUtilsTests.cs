using AcxiomCRM.Common;
using Xunit;

namespace AcxiomCRM.Tests.Unit;

public class PhoneUtilsTests
{
    [Theory]
    [InlineData("9876543210")]
    [InlineData("+919876543210")]
    [InlineData("919876543210")]
    [InlineData("09876543210")]
    [InlineData("98765 43210")]
    [InlineData("+91-98765-43210")]
    [InlineData("6123456789")]
    [InlineData("7123456789")]
    [InlineData("8123456789")]
    public void IsValidIndianPhone_ValidNumbers_ReturnsTrue(string phone)
    {
        var result = PhoneUtils.IsValidIndianPhone(phone);
        Assert.True(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("1234567890")] // Starts with 1
    [InlineData("5555555555")] // Starts with 5
    [InlineData("987654321")]  // 9 digits
    [InlineData("98765432101")] // 11 digits without prefix
    [InlineData("abcdefghij")]
    [InlineData("98765abcde")]
    public void IsValidIndianPhone_InvalidNumbers_ReturnsFalse(string? phone)
    {
        var result = PhoneUtils.IsValidIndianPhone(phone);
        Assert.False(result);
    }

    [Theory]
    [InlineData("+919876543210", "9876543210")]
    [InlineData("919876543210", "9876543210")]
    [InlineData("09876543210", "9876543210")]
    [InlineData("98765 43210", "9876543210")]
    [InlineData("+91-98765-43210", "9876543210")]
    public void NormalizePhone_VariousFormats_ReturnsTenDigits(string input, string expected)
    {
        var result = PhoneUtils.NormalizePhone(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("TEST@EXAMPLE.COM", "test@example.com")]
    [InlineData("  user@domain.in  ", "user@domain.in")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void NormalizeEmail_VariousFormats_TrimsAndLowercases(string? input, string expected)
    {
        var result = PhoneUtils.NormalizeEmail(input);
        Assert.Equal(expected, result);
    }
}

