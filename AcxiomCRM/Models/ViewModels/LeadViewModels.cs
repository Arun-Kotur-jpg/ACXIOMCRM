using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Common;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;

namespace AcxiomCRM.Models.ViewModels;

public class LeadListViewModel
{
    public PagedResult<Lead> Leads { get; set; } = new();
    public string? SearchTerm { get; set; }
    public LeadStatus? StatusFilter { get; set; }
    public LeadPriority? PriorityFilter { get; set; }
    public LeadSource? SourceFilter { get; set; }
    public string? AssigneeFilter { get; set; }
    public string SortBy { get; set; } = "DateDesc";
    public SelectList? AvailableAssignees { get; set; }
}

public class LeadCreateEditViewModel
{
    public int LeadId { get; set; }

    public string? LeadCode { get; set; }

    [Required(ErrorMessage = "Lead Name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Lead Name must be between 2 and 150 characters.")]
    [Display(Name = "Lead Name")]
    public string LeadName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Phone number is required.")]
    [IndianPhone(ErrorMessage = "Enter a valid 10-digit Indian phone number (e.g. 9876543210).")]
    [Display(Name = "Phone Number")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "Company Name cannot exceed 150 characters.")]
    [Display(Name = "Company Name")]
    public string? CompanyName { get; set; }

    public LeadSource Source { get; set; } = LeadSource.Website;

    public LeadStatus Status { get; set; } = LeadStatus.New;

    public LeadPriority Priority { get; set; } = LeadPriority.Medium;

    [Range(0, 1000000000, ErrorMessage = "Expected Value must be 0 or greater.")]
    [Display(Name = "Expected Value (INR)")]
    public decimal? ExpectedValue { get; set; }

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    public string? Notes { get; set; }

    [Required(ErrorMessage = "Assigned To is required.")]
    [Display(Name = "Assigned To")]
    public string AssignedTo { get; set; } = string.Empty;

    public SelectList? AvailableAssignees { get; set; }
}

public class LeadDetailsViewModel
{
    public Lead Lead { get; set; } = null!;
    public List<Opportunity> RelatedOpportunities { get; set; } = new();
    public List<FollowUp> RelatedFollowUps { get; set; } = new();
    public List<Activity> RelatedActivities { get; set; } = new();
    public List<AuditLog> AuditLogs { get; set; } = new();
}

public class LeadConvertViewModel
{
    public int LeadId { get; set; }

    [Display(Name = "Lead Code")]
    public string LeadCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer Name is required.")]
    [Display(Name = "Customer Name")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer Phone is required.")]
    [IndianPhone(ErrorMessage = "Enter a valid 10-digit Indian phone number.")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "Company Name")]
    public string? CompanyName { get; set; }

    [Display(Name = "Create Related Opportunity")]
    public bool CreateOpportunity { get; set; } = true;

    [Display(Name = "Opportunity Name")]
    public string? OpportunityName { get; set; }

    [Display(Name = "Opportunity Amount (INR)")]
    [Range(0.01, 1000000000, ErrorMessage = "Amount must be greater than 0.")]
    public decimal? OpportunityAmount { get; set; }

    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int OpportunityProbability { get; set; } = 25;

    [Display(Name = "Expected Close Date")]
    public DateTime? ExpectedCloseDate { get; set; }

    [Required(ErrorMessage = "Assigned Owner is required.")]
    [Display(Name = "Assign Owner")]
    public string AssignedTo { get; set; } = string.Empty;

    public SelectList? AvailableAssignees { get; set; }
}

