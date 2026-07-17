using System.Text.Json;
using System.Text.Json.Nodes;
using RestaurantAPI.Services.AI.Contracts;
using RestaurantAPI.Services.AI.Tools.Menu;
using RestaurantAPI.Services.AI.Tools.Restaurant;

namespace RestaurantAPI.Services.AI.Tools;

public class ToolDispatcher : IToolDispatcher
{
    private readonly GetMenuCatalogTool _catalogTool;
    private readonly GetMenuItemDetailsTool _detailsTool;
    private readonly GetMenuItemsByIngredientTool _ingredientTool;
    private readonly GetRestaurantInfoTool _restaurantInfoTool;

    public ToolDispatcher(
        GetMenuCatalogTool catalogTool,
        GetMenuItemDetailsTool detailsTool,
        GetMenuItemsByIngredientTool ingredientTool,
        GetRestaurantInfoTool restaurantInfoTool)
    {
        _catalogTool = catalogTool;
        _detailsTool = detailsTool;
        _ingredientTool = ingredientTool;
        _restaurantInfoTool = restaurantInfoTool;
    }

    public async Task<string> ExecuteAsync(string toolName, string arguments)
    {
        JsonObject? args = null;
        if (!string.IsNullOrWhiteSpace(arguments))
        {
            try { args = JsonNode.Parse(arguments)?.AsObject(); }
            catch { /* malformed JSON — leave args null */ }
        }

        return toolName switch
        {
            "get_restaurant_info" =>
                await _restaurantInfoTool.ExecuteAsync(),

            "get_menu_catalog" =>
                await _catalogTool.ExecuteAsync(),

            "get_menu_item_details" =>
                await _detailsTool.ExecuteAsync(
                    args?["name"]?.GetValue<string>() ?? string.Empty),

            "get_menu_items_by_ingredient" =>
                await _ingredientTool.ExecuteAsync(
                    args?["ingredientName"]?.GetValue<string>() ?? string.Empty,
                    args?["exclude"]?.GetValue<bool>() ?? false),

            _ => JsonSerializer.Serialize(new { error = $"Unknown tool: {toolName}" })
        };
    }
}
