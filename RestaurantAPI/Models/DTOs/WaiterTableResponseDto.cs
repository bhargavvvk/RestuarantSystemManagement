namespace RestaurantAPI.Models.DTOs;

public class WaiterTableResponseDto
{
    public int TableId { get; set; }

    public string TableNumber { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}
