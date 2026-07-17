namespace RestaurantAPI.Models.DTOs;

/// <summary>
/// Full replace of all ingredients for a menu item.
/// Send an empty list to clear all ingredients.
/// </summary>
public class SetMenuItemIngredientsDto
{
    public List<MenuItemIngredientDto> Ingredients { get; set; } = new();
}
