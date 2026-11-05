using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AcxiomCRM.Models.Enums;

namespace AcxiomCRM.Models.Entities;

public class FollowUp
{
    [Key]
    public int FollowUpId { get; set; }

    public int? CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public virtual Customer? Customer { get; set; }

    public int? LeadId { get; set; }

    [ForeignKey(nameof(LeadId))]
    public virtual Lead? Lead { get; set; }

    public int? OpportunityId { get; set; }

    [ForeignKey(nameof(OpportunityId))]
    public virtual Opportunity? Opportunity { get; set; }

    [Required]
    public DateTime FollowUpDate { get; set; }

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    public FollowUpType FollowUpType { get; set; } = FollowUpType.Call;

    public FollowUpStatus Status { get; set; } = FollowUpStatus.Planned;

    [MaxLength(1000)]
    public string? Remarks { get; set; }

    [Required]
    [MaxLength(450)]
    public string AssignedTo { get; set; } = string.Empty;

    [ForeignKey(nameof(AssignedTo))]
    public virtual ApplicationUser? AssignedUser { get; set; }

    [NotMapped]
    public ApplicationUser? AssignedToUser => AssignedUser;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? CompletedDate { get; set; }
}

