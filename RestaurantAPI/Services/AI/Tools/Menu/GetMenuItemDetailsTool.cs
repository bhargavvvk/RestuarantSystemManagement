using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;

namespace RestaurantAPI.Services.AI.Tools.Menu;

/// <summary>
/// Fetches full details of a menu item by name (fuzzy / partial match).
/// Returns name, category, price, food type, description,
/// ingredients (with quantity/unit), and nutrition info.
/// </summary>
public class GetMenuItemDetailsTool
{
    private readonly RestaurantContext _context;

    public GetMenuItemDetailsTool(RestaurantContext context)
    {
        _context = context;
    }

    public async Task<string> ExecuteAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return JsonSerializer.Serialize(new { error = "name parameter is required." });

        var normalized = name.Trim().ToUpper();

        // Exact match first, then starts-with, then contains
        var item = await _context.MenuItems
            .Include(m => m.Category)
            .Include(m => m.MenuItemIngredients!)
                .ThenInclude(mi => mi.Ingredient)
            .Include(m => m.Nutrition)
            .Where(m => !m.IsDeleted && m.IsAvailable)
            .OrderBy(m =>
                m.Name.ToUpper() == normalized ? 0 :
                m.Name.ToUpper().StartsWith(normalized) ? 1 : 2)
            .FirstOrDefaultAsync(m =>
                m.Name.ToUpper() == normalized ||
                m.Name.ToUpper().Contains(normalized));

        if (item == null)
            return JsonSerializer.Serialize(new
            {
                error = $"No available menu item found matching '{name}'."
            });

        var result = new
        {
            id = item.Id,
            name = item.Name,
            category = item.Category?.Name,
            price = item.Price,
            foodType = item.FoodType.ToString(),
            description = item.Description,
            ingredients = item.MenuItemIngredients?.Select(mi => new
            {
                name = mi.Ingredient?.Name,
                approxQuantity = mi.ApproxQuantity,
                unit = mi.Unit
            }).ToList(),
            nutrition = item.Nutrition == null ? null : new
            {
                calories = item.Nutrition.Calories,
                protein = item.Nutrition.Protein,
                carbohydrates = item.Nutrition.Carbohydrates,
                fat = item.Nutrition.Fat,
                fiber = item.Nutrition.Fiber,
                sugar = item.Nutrition.Sugar,
                sodium = item.Nutrition.Sodium
            }
        };

        return JsonSerializer.Serialize(result);
    }
}
