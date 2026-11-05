using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AcxiomCRM.Models.Enums;

namespace AcxiomCRM.Models.Entities;

public class Lead
{
    [Key]
    public int LeadId { get; set; }

    [Required]
    [MaxLength(20)]
    public string LeadCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string LeadName { get; set; } = string.Empty;

    [MaxLength(150)]
    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? CompanyName { get; set; }

    public LeadSource Source { get; set; } = LeadSource.Website;

    public LeadStatus Status { get; set; } = LeadStatus.New;

    public LeadPriority Priority { get; set; } = LeadPriority.Medium;

    [Range(0, 1000000000)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal? ExpectedValue { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Required]
    [MaxLength(450)]
    public string AssignedTo { get; set; } = string.Empty;

    [ForeignKey(nameof(AssignedTo))]
    public virtual ApplicationUser? AssignedUser { get; set; }

    [NotMapped]
    public ApplicationUser? AssignedToUser => AssignedUser;

    public int? CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public virtual Customer? Customer { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? ModifiedDate { get; set; }

    [NotMapped]
    public DateTime? UpdatedDate => ModifiedDate;

    [MaxLength(100)]
    public string? ModifiedBy { get; set; }

    public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
    public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
}

