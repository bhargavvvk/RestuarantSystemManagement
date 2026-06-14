namespace RestaurantAPI.Models.DTOs;

public class CategoryResponseDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsAvailable { get; set; }
}
