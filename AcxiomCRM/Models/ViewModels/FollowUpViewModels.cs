using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;

namespace AcxiomCRM.Models.ViewModels;

public class FollowUpListViewModel
{
    public PagedResult<FollowUp> FollowUps { get; set; } = new();
    public string? SearchTerm { get; set; }
    public FollowUpStatus? StatusFilter { get; set; }
    public FollowUpType? TypeFilter { get; set; }
    public string? AssigneeFilter { get; set; }
    public string FilterScope { get; set; } = "All"; // All, Upcoming, Overdue, Today
    public SelectList? AvailableAssignees { get; set; }
    public int UpcomingCount { get; set; }
    public int OverdueCount { get; set; }
}

public class FollowUpCreateEditViewModel
{
    public int FollowUpId { get; set; }

    [Display(Name = "Customer (Optional)")]
    public int? CustomerId { get; set; }

    [Display(Name = "Lead (Optional)")]
    public int? LeadId { get; set; }

    [Display(Name = "Opportunity (Optional)")]
    public int? OpportunityId { get; set; }

    [Required(ErrorMessage = "Follow-up date and time is required.")]
    [Display(Name = "Follow-Up Date & Time")]
    public DateTime FollowUpDate { get; set; } = DateTime.Now.AddHours(2);

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Subject must be between 2 and 200 characters.")]
    public string Subject { get; set; } = string.Empty;

    [Display(Name = "Type")]
    public FollowUpType FollowUpType { get; set; } = FollowUpType.Call;

    public FollowUpStatus Status { get; set; } = FollowUpStatus.Planned;

    [StringLength(1000, ErrorMessage = "Remarks cannot exceed 1000 characters.")]
    public string? Remarks { get; set; }

    [Required(ErrorMessage = "Assigned To is required.")]
    [Display(Name = "Assigned To")]
    public string AssignedTo { get; set; } = string.Empty;

    public SelectList? AvailableCustomers { get; set; }
    public SelectList? AvailableLeads { get; set; }
    public SelectList? AvailableOpportunities { get; set; }
    public SelectList? AvailableAssignees { get; set; }
}

