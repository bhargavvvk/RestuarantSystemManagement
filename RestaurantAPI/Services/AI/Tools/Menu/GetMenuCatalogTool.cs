using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;

namespace RestaurantAPI.Services.AI.Tools.Menu;

/// <summary>
/// Returns a lightweight catalog of all available menu items:
/// just the name and category.
/// Used by the AI to know what items exist before fetching details.
/// </summary>
public class GetMenuCatalogTool
{
    private readonly RestaurantContext _context;

    public GetMenuCatalogTool(RestaurantContext context)
    {
        _context = context;
    }

    public async Task<string> ExecuteAsync()
    {
        var items = await _context.MenuItems
            .Include(m => m.Category)
            .Where(m => !m.IsDeleted && m.IsAvailable)
            .OrderBy(m => m.Category!.Name)
            .ThenBy(m => m.Name)
            .Select(m => new
            {
                name = m.Name,
                category = m.Category!.Name,
                foodType = m.FoodType.ToString(),
                price = m.Price
            })
            .ToListAsync();

        if (items.Count == 0)
            return JsonSerializer.Serialize(new { message = "No menu items are currently available." });

        return JsonSerializer.Serialize(new { items });
    }
}
