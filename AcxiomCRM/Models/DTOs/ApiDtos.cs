using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Common;
using AcxiomCRM.Models.Enums;

namespace AcxiomCRM.Models.DTOs;

public class LoginRequestDto
{
    [Required]
    public string EmailOrUsername { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class CustomerDto
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string Status { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateCustomerDto
{
    [Required(ErrorMessage = "Customer Name is required.")]
    [StringLength(150, MinimumLength = 2)]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [IndianPhone]
    public string Phone { get; set; } = string.Empty;

    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
    public string? OwnerId { get; set; }
}

public class UpdateCustomerDto
{
    [Required(ErrorMessage = "Customer Name is required.")]
    [StringLength(150, MinimumLength = 2)]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [IndianPhone]
    public string Phone { get; set; } = string.Empty;

    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public CustomerStatus Status { get; set; }
    public string? OwnerId { get; set; }
}

public class LeadDto
{
    public int LeadId { get; set; }
    public string LeadCode { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public decimal? ExpectedValue { get; set; }
    public string? Notes { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public string? AssignedUserName { get; set; }
    public int? CustomerId { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateLeadDto
{
    [Required(ErrorMessage = "Lead Name is required.")]
    [StringLength(150, MinimumLength = 2)]
    public string LeadName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Phone is required.")]
    [IndianPhone]
    public string Phone { get; set; } = string.Empty;

    public string? CompanyName { get; set; }
    public LeadSource Source { get; set; } = LeadSource.Website;
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public LeadPriority Priority { get; set; } = LeadPriority.Medium;

    [Range(0, 1000000000)]
    public decimal? ExpectedValue { get; set; }

    public string? Notes { get; set; }
    public string? AssignedTo { get; set; }
}

public class UpdateLeadDto
{
    [Required(ErrorMessage = "Lead Name is required.")]
    [StringLength(150, MinimumLength = 2)]
    public string LeadName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Phone is required.")]
    [IndianPhone]
    public string Phone { get; set; } = string.Empty;

    public string? CompanyName { get; set; }
    public LeadSource Source { get; set; }
    public LeadStatus Status { get; set; }
    public LeadPriority Priority { get; set; }

    [Range(0, 1000000000)]
    public decimal? ExpectedValue { get; set; }

    public string? Notes { get; set; }
    public string? AssignedTo { get; set; }
}

public class OpportunityDto
{
    public int OpportunityId { get; set; }
    public string OpportunityName { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? LeadId { get; set; }
    public decimal Amount { get; set; }
    public string Stage { get; set; } = string.Empty;
    public int Probability { get; set; }
    public decimal WeightedPipeline { get; set; }
    public DateTime ExpectedCloseDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? Notes { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public string? AssignedUserName { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateOpportunityDto
{
    [Required(ErrorMessage = "Opportunity Name is required.")]
    [StringLength(150, MinimumLength = 2)]
    public string OpportunityName { get; set; } = string.Empty;

    [Required(ErrorMessage = "CustomerId is required.")]
    public int CustomerId { get; set; }

    public int? LeadId { get; set; }

    [Required(ErrorMessage = "Amount must be greater than 0.")]
    [Range(0.01, 1000000000, ErrorMessage = "Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;

    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int Probability { get; set; } = 20;

    [Required(ErrorMessage = "ExpectedCloseDate is required.")]
    public DateTime ExpectedCloseDate { get; set; }

    public OpportunityStatus Status { get; set; } = OpportunityStatus.Open;
    public string? Source { get; set; }
    public string? Notes { get; set; }
    public string? AssignedTo { get; set; }
}

public class UpdateOpportunityDto
{
    [Required(ErrorMessage = "Opportunity Name is required.")]
    [StringLength(150, MinimumLength = 2)]
    public string OpportunityName { get; set; } = string.Empty;

    [Required(ErrorMessage = "CustomerId is required.")]
    public int CustomerId { get; set; }

    public int? LeadId { get; set; }

    [Required(ErrorMessage = "Amount must be greater than 0.")]
    [Range(0.01, 1000000000, ErrorMessage = "Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    public OpportunityStage Stage { get; set; }

    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int Probability { get; set; }

    [Required(ErrorMessage = "ExpectedCloseDate is required.")]
    public DateTime ExpectedCloseDate { get; set; }

    public OpportunityStatus Status { get; set; }
    public string? Source { get; set; }
    public string? Notes { get; set; }
    public string? AssignedTo { get; set; }
}

public class FollowUpDto
{
    public int FollowUpId { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? LeadId { get; set; }
    public string? LeadName { get; set; }
    public int? OpportunityId { get; set; }
    public string? OpportunityName { get; set; }
    public DateTime FollowUpDate { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string FollowUpType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public string? AssignedUserName { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateFollowUpDto
{
    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }
    public int? OpportunityId { get; set; }

    [Required(ErrorMessage = "FollowUpDate is required.")]
    public DateTime FollowUpDate { get; set; }

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(200, MinimumLength = 2)]
    public string Subject { get; set; } = string.Empty;

    public FollowUpType FollowUpType { get; set; } = FollowUpType.Call;
    public FollowUpStatus Status { get; set; } = FollowUpStatus.Planned;
    public string? Remarks { get; set; }
    public string? AssignedTo { get; set; }
}

public class PipelineReportDto
{
    public decimal TotalOpenValue { get; set; }
    public decimal TotalWeightedValue { get; set; }
    public int TotalOpenCount { get; set; }
    public Dictionary<string, decimal> StageValues { get; set; } = new();
}

public class ApiErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}

