using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;

namespace AcxiomCRM.Models.ViewModels;

public class ActivityListViewModel
{
    public PagedResult<Activity> Activities { get; set; } = new();
    public string? SearchTerm { get; set; }
    public ActivityType? TypeFilter { get; set; }
    public ActivityStatus? StatusFilter { get; set; }
    public string? AssigneeFilter { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public SelectList? AvailableAssignees { get; set; }
}

public class ActivityCreateEditViewModel
{
    public int ActivityId { get; set; }

    [Display(Name = "Activity Type")]
    public ActivityType ActivityType { get; set; } = ActivityType.Call;

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Subject must be between 2 and 200 characters.")]
    public string Subject { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Activity Date is required.")]
    [Display(Name = "Activity Date & Time")]
    public DateTime ActivityDate { get; set; } = DateTime.Now;

    [Display(Name = "Customer (Optional)")]
    public int? CustomerId { get; set; }

    [Display(Name = "Lead (Optional)")]
    public int? LeadId { get; set; }

    [Display(Name = "Opportunity (Optional)")]
    public int? OpportunityId { get; set; }

    public ActivityStatus Status { get; set; } = ActivityStatus.Completed;

    [Required(ErrorMessage = "Assigned To is required.")]
    [Display(Name = "Assigned To")]
    public string AssignedTo { get; set; } = string.Empty;

    public SelectList? AvailableCustomers { get; set; }
    public SelectList? AvailableLeads { get; set; }
    public SelectList? AvailableOpportunities { get; set; }
    public SelectList? AvailableAssignees { get; set; }
}

