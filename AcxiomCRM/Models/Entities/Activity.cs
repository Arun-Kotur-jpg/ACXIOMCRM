using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AcxiomCRM.Models.Enums;

namespace AcxiomCRM.Models.Entities;

public class Activity
{
    [Key]
    public int ActivityId { get; set; }

    public ActivityType ActivityType { get; set; } = ActivityType.Call;

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public DateTime ActivityDate { get; set; }

    public int? CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public virtual Customer? Customer { get; set; }

    public int? LeadId { get; set; }

    [ForeignKey(nameof(LeadId))]
    public virtual Lead? Lead { get; set; }

    public int? OpportunityId { get; set; }

    [ForeignKey(nameof(OpportunityId))]
    public virtual Opportunity? Opportunity { get; set; }

    public ActivityStatus Status { get; set; } = ActivityStatus.Completed;

    [Required]
    [MaxLength(450)]
    public string AssignedTo { get; set; } = string.Empty;

    [ForeignKey(nameof(AssignedTo))]
    public virtual ApplicationUser? AssignedUser { get; set; }

    [NotMapped]
    public ApplicationUser? AssignedToUser => AssignedUser;

    [NotMapped]
    public ApplicationUser? PerformedByUser => AssignedUser;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;
}

