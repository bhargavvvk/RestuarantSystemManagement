namespace RestaurantAPI.Models.DTOs;

public class TableResponseDto
{
     public int Id { get; set; }

    public string TableNumber { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public string QrIdentifier { get; set; } = string.Empty;

    public TableStatus Status { get; set; }

    public int? AssignedWaiterId { get; set; }
}
