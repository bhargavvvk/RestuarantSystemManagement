using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.Models;

public enum AuditAction
{
    Created,
    Updated,
    Deleted,
    Disabled,
    Enabled,
    Cancelled
}

public class AuditLog
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Entity name is required.")]
    public string EntityName { get; set; } = string.Empty;

    // Internal database primary key
    [Required(ErrorMessage = "Entity ID is required.")]
    public string EntityId { get; set; } = string.Empty;

    // Human-readable identifier shown to administrators
    [Required(ErrorMessage = "Entity identifier is required.")]
    public string EntityIdentifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "Action is required.")]
    public AuditAction Action { get; set; }

    // JSON snapshot before the change
    public string? OldValues { get; set; }

    // JSON snapshot after the change
    public string? NewValues { get; set; }

    public string? Remarks { get; set; }

    public DateTime PerformedAt { get; set; }
}