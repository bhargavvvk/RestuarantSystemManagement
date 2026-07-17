using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.Models;

public class MenuItemIngredient
{
    public int Id { get; set; }
    [Required]
    public int MenuItemId { get; set; }
    [Required]
    public int IngredientId { get; set; }
    public decimal? ApproxQuantity { get; set; }
    [StringLength(20)]
    public string? Unit { get; set; }
    public MenuItem? MenuItem { get; set; }
    public Ingredient? Ingredient { get; set; }
}