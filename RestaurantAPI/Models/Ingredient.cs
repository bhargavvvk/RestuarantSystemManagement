using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.Models;

public class Ingredient
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    public ICollection<MenuItemIngredient>? MenuItemIngredients { get; set; }
}
