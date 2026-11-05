using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [MaxLength(450)]
    public string? ManagerId { get; set; }

    [ForeignKey(nameof(ManagerId))]
    public virtual ApplicationUser? Manager { get; set; }

    public virtual ICollection<ApplicationUser> Subordinates { get; set; } = new List<ApplicationUser>();

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginDate { get; set; }

    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public virtual ICollection<Lead> Leads { get; set; } = new List<Lead>();
    public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
    public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
}

