
using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.Models.DTOs;

public class AddMenuItemDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public decimal Price { get; set; }

    public string? Description { get; set; }

    public bool IsAvailable { get; set; }

    public IFormFile? Image { get; set; }
}