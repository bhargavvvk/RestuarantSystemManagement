namespace RestaurantAPI.Models.DTOs;

public class AddCategoryDto
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsAvailable { get; set; }
}