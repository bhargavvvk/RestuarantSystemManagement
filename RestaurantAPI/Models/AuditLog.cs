using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.Models;

public enum AuditAction
{
    Created,
    Updated,
    Deleted,
    Disabled,
    Enabled,
    Cancelled,
}

public class AuditLog
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Entity name is required.")]
    public string EntityName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Entity ID is required.")]
    public string EntityId { get; set; } = string.Empty;
    [Required(ErrorMessage = "Action is required.")]
    public AuditAction Action { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? Remarks { get; set; }

    public DateTime PerformedAt { get; set; }
}