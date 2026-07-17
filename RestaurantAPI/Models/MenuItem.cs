using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.Models;
public enum FoodType
{
    Veg,
    NonVeg
}

public class MenuItem
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Menu item name is required.")]
    [StringLength(50, ErrorMessage = "Menu item name cannot exceed 50 characters.")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Price cannot be negative.")]
    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsAvailable { get; set; } = true;

    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; }
    [Required]
    public FoodType FoodType { get; set; }

    public ICollection<CartItem>? CartItems { get; set; }

    public ICollection<OrderItem>? OrderItems { get; set; }

    public Category? Category { get; set; }

    public ICollection<MenuItemIngredient>? MenuItemIngredients { get; set; }

    public MenuItemNutrition? Nutrition { get; set; }
}