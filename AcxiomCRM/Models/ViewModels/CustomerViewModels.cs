using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Common;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;

namespace AcxiomCRM.Models.ViewModels;

public class CustomerListViewModel
{
    public PagedResult<Customer> Customers { get; set; } = new();
    public string? SearchTerm { get; set; }
    public CustomerStatus? StatusFilter { get; set; }
    public string? OwnerFilter { get; set; }
    public string SortBy { get; set; } = "DateDesc";
    public SelectList? AvailableOwners { get; set; }
}

public class CustomerCreateEditViewModel
{
    public int CustomerId { get; set; }

    public string? CustomerCode { get; set; }

    [Required(ErrorMessage = "Customer Name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Customer Name must be between 2 and 150 characters.")]
    [Display(Name = "Customer Name")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [IndianPhone(ErrorMessage = "Enter a valid 10-digit Indian phone number (e.g. 9876543210).")]
    [Display(Name = "Phone Number")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "Company Name cannot exceed 150 characters.")]
    [Display(Name = "Company Name")]
    public string? CompanyName { get; set; }

    [StringLength(250, ErrorMessage = "Address cannot exceed 250 characters.")]
    public string? Address { get; set; }

    [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
    public string? City { get; set; }

    [StringLength(100, ErrorMessage = "State cannot exceed 100 characters.")]
    public string? State { get; set; }

    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    [Required(ErrorMessage = "Owner / Assigned To is required.")]
    [Display(Name = "Assigned Owner")]
    public string OwnerId { get; set; } = string.Empty;

    public SelectList? AvailableOwners { get; set; }
}

public class CustomerDetailsViewModel
{
    public Customer Customer { get; set; } = null!;
    public List<Lead> RelatedLeads { get; set; } = new();
    public List<Opportunity> RelatedOpportunities { get; set; } = new();
    public List<FollowUp> RelatedFollowUps { get; set; } = new();
    public List<Activity> RelatedActivities { get; set; } = new();
    public List<AuditLog> AuditLogs { get; set; } = new();
}

