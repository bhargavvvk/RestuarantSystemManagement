namespace RestaurantAPI.Models.DTOs;

public class TableStatusResponseDto
{
    public string TableNumber { get; set; } = string.Empty;

    public bool IsAvailable { get; set; }

    public bool HasActiveSession { get; set; }
}

