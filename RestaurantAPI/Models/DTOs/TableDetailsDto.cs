namespace RestaurantAPI.Models.DTOs;

public class TableDetailsDto
{
     public int TableId { get; set; }

    public string TableNumber { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int? AssignedWaiterId { get; set; }

    public string? AssignedWaiterName { get; set; }

    public DateTime? SessionStartedAt { get; set; }
}
