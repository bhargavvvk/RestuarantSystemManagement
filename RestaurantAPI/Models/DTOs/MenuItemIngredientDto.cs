using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.Models.DTOs;

/// <summary>
/// Used when adding/updating an ingredient entry on a menu item.
/// Either provide an existing IngredientId OR an inline NewIngredient to create on the fly.
/// </summary>
public class MenuItemIngredientDto
{
    // Provide this to reference an existing ingredient
    public int? IngredientId { get; set; }

    // Provide this to create a brand-new ingredient inline
    public AddIngredientDto? NewIngredient { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "ApproxQuantity must be greater than zero.")]
    public decimal? ApproxQuantity { get; set; }

    [StringLength(20, ErrorMessage = "Unit cannot exceed 20 characters.")]
    public string? Unit { get; set; }
}

/// <summary>Response shape for a single ingredient on a menu item.</summary>
public class MenuItemIngredientResponseDto
{
    public int Id { get; set; }
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal? ApproxQuantity { get; set; }
    public string? Unit { get; set; }
}
