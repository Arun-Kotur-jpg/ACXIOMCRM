using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;

namespace AcxiomCRM.Models.ViewModels;

public class OpportunityListViewModel
{
    public PagedResult<Opportunity> Opportunities { get; set; } = new();
    public string? SearchTerm { get; set; }
    public OpportunityStage? StageFilter { get; set; }
    public OpportunityStatus? StatusFilter { get; set; }
    public string? AssigneeFilter { get; set; }
    public string SortBy { get; set; } = "DateDesc";
    public SelectList? AvailableAssignees { get; set; }
}

public class OpportunityCreateEditViewModel
{
    public int OpportunityId { get; set; }

    [Required(ErrorMessage = "Opportunity Name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Opportunity Name must be between 2 and 150 characters.")]
    [Display(Name = "Opportunity Name")]
    public string OpportunityName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer is required.")]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Display(Name = "Related Lead (Optional)")]
    public int? LeadId { get; set; }

    [Required(ErrorMessage = "Opportunity Amount must be greater than 0.")]
    [Range(0.01, 1000000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    [Display(Name = "Amount (INR)")]
    public decimal Amount { get; set; }

    public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;

    [Required(ErrorMessage = "Probability is required.")]
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int Probability { get; set; } = 20;

    [Required(ErrorMessage = "Expected Close Date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Expected Close Date")]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);

    public OpportunityStatus Status { get; set; } = OpportunityStatus.Open;

    [StringLength(100, ErrorMessage = "Source cannot exceed 100 characters.")]
    public string? Source { get; set; }

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    public string? Notes { get; set; }

    [Required(ErrorMessage = "Assigned User is required.")]
    [Display(Name = "Assigned To")]
    public string AssignedTo { get; set; } = string.Empty;

    public SelectList? AvailableCustomers { get; set; }
    public SelectList? AvailableLeads { get; set; }
    public SelectList? AvailableAssignees { get; set; }
}

public class OpportunityDetailsViewModel
{
    public Opportunity Opportunity { get; set; } = null!;
    public List<FollowUp> RelatedFollowUps { get; set; } = new();
    public List<Activity> RelatedActivities { get; set; } = new();
    public List<AuditLog> AuditLogs { get; set; } = new();
}

