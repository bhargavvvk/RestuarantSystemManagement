using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.Models.DTOs;

public class UpdateIngredientDto
{
    [Required(ErrorMessage = "Ingredient name is required.")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }
}
