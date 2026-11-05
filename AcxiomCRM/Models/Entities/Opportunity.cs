using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AcxiomCRM.Models.Enums;

namespace AcxiomCRM.Models.Entities;

public class Opportunity
{
    [Key]
    public int OpportunityId { get; set; }

    [Required]
    [MaxLength(150)]
    public string OpportunityName { get; set; } = string.Empty;

    [Required]
    public int CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public virtual Customer? Customer { get; set; }

    public int? LeadId { get; set; }

    [ForeignKey(nameof(LeadId))]
    public virtual Lead? Lead { get; set; }

    [Required]
    [Range(0.01, 1000000000)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;

    [Range(0, 100)]
    public int Probability { get; set; }

    public DateTime ExpectedCloseDate { get; set; }

    public OpportunityStatus Status { get; set; } = OpportunityStatus.Open;

    [MaxLength(100)]
    public string? Source { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

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

    public DateTime? ModifiedDate { get; set; }

    [NotMapped]
    public DateTime? UpdatedDate => ModifiedDate;

    [MaxLength(100)]
    public string? ModifiedBy { get; set; }

    [NotMapped]
    public decimal WeightedPipeline => Amount * Probability / 100m;

    [NotMapped]
    public decimal WeightedAmount => WeightedPipeline;

    public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
}

