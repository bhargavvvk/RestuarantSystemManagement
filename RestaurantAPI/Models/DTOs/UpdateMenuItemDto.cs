namespace RestaurantAPI.Models.DTOs;

public class UpdateMenuItemDto
{
    public string Name { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public decimal Price { get; set; }

    public string? Description { get; set; }
}
