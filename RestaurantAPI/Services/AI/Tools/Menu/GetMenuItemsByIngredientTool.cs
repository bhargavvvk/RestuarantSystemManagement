using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;

namespace RestaurantAPI.Services.AI.Tools.Menu;

/// <summary>
/// Returns all available menu items that contain (or do NOT contain)
/// a given ingredient. Useful for allergy / dietary queries.
/// </summary>
public class GetMenuItemsByIngredientTool
{
    private readonly RestaurantContext _context;

    public GetMenuItemsByIngredientTool(RestaurantContext context)
    {
        _context = context;
    }

    /// <param name="ingredientName">Ingredient to search for (partial match, case-insensitive).</param>
    /// <param name="exclude">
    /// When true, returns items that do NOT contain the ingredient (allergy-safe list).
    /// When false (default), returns items that DO contain it.
    /// </param>
    public async Task<string> ExecuteAsync(string ingredientName, bool exclude = false)
    {
        if (string.IsNullOrWhiteSpace(ingredientName))
            return JsonSerializer.Serialize(new { error = "ingredientName parameter is required." });

        var normalized = ingredientName.Trim().ToUpper();

        // All available items with their ingredients
        var allItems = await _context.MenuItems
            .Include(m => m.Category)
            .Include(m => m.MenuItemIngredients!)
                .ThenInclude(mi => mi.Ingredient)
            .Where(m => !m.IsDeleted && m.IsAvailable)
            .ToListAsync();

        var result = allItems.Where(m =>
        {
            var hasIngredient = m.MenuItemIngredients != null &&
                m.MenuItemIngredients.Any(mi =>
                    mi.Ingredient != null &&
                    mi.Ingredient.Name.ToUpper().Contains(normalized));

            return exclude ? !hasIngredient : hasIngredient;
        })
        .OrderBy(m => m.Category?.Name)
        .ThenBy(m => m.Name)
        .Select(m => new
        {
            name = m.Name,
            category = m.Category?.Name,
            price = m.Price,
            foodType = m.FoodType.ToString(),
            ingredients = m.MenuItemIngredients?.Select(mi => mi.Ingredient?.Name).ToList()
        })
        .ToList();

        if (result.Count == 0)
        {
            var msg = exclude
                ? $"All available menu items contain '{ingredientName}', or no ingredient data is available."
                : $"No available menu items found containing '{ingredientName}'.";
            return JsonSerializer.Serialize(new { message = msg });
        }

        return JsonSerializer.Serialize(new
        {
            ingredientFilter = ingredientName,
            exclude,
            count = result.Count,
            items = result
        });
    }
}
