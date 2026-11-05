using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models.Entities;

public class AuditLog
{
    [Key]
    public int AuditLogId { get; set; }

    [MaxLength(450)]
    public string? UserId { get; set; }

    [MaxLength(100)]
    public string? UserName { get; set; }

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string EntityName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? RecordId { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    [Required]
    [MaxLength(50)]
    public string Result { get; set; } = "Success";

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    [MaxLength(2000)]
    public string? Details { get; set; }

    [NotMapped]
    public string ActionName { get => Action; set => Action = value; }

    [NotMapped]
    public string PerformedByName { get => UserName ?? "System"; set => UserName = value; }

    [NotMapped]
    public DateTime Timestamp => CreatedDate;

    [NotMapped]
    public string ChangedByUser => UserName ?? "System";

    [NotMapped]
    public string FieldName => Details ?? "";
}

