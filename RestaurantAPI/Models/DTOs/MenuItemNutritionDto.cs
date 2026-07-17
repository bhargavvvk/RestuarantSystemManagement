namespace RestaurantAPI.Models.DTOs;

/// <summary>
/// Used when setting nutrition for a menu item.
/// All fields are optional; omit the entire object to have no nutrition data.
/// All provided values must be >= 0.
/// </summary>
public class MenuItemNutritionDto
{
    public decimal? Calories { get; set; }
    public decimal? Protein { get; set; }
    public decimal? Carbohydrates { get; set; }
    public decimal? Fat { get; set; }
    public decimal? Fiber { get; set; }
    public decimal? Sugar { get; set; }
    public decimal? Sodium { get; set; }
}

/// <summary>Response shape for nutrition info.</summary>
public class MenuItemNutritionResponseDto
{
    public int Id { get; set; }
    public decimal? Calories { get; set; }
    public decimal? Protein { get; set; }
    public decimal? Carbohydrates { get; set; }
    public decimal? Fat { get; set; }
    public decimal? Fiber { get; set; }
    public decimal? Sugar { get; set; }
    public decimal? Sodium { get; set; }
}
