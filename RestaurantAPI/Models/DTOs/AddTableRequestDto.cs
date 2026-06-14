namespace RestaurantAPI.Models.DTOs;

public class AddTableRequestDto
{
    public string TableNumber { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public int? AssignedWaiterId { get; set; }
}