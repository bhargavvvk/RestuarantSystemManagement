using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.Models;

public enum TableStatus
{
    Available,
    Unavailable
}

public class RestaurantTable
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Table number is required.")]
    [StringLength(10, ErrorMessage = "Table number cannot exceed 10 characters.")]
    public string TableNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "QR identifier is required.")]
    public string QrIdentifier { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Capacity must be greater than 0.")]
    public int Capacity { get; set; }

    public TableStatus Status { get; set; } = TableStatus.Available;

    public int? AssignedWaiterId { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    public User? AssignedWaiter { get; set; }

    public ICollection<DiningSession>? DiningSessions { get; set; }
}