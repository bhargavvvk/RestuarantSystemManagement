using System.Text.Json.Nodes;
using RestaurantAPI.Services.AI.Contracts.Models;

namespace RestaurantAPI.Services.AI.Tools;

/// <summary>
/// Builds the list of ClaudeTool definitions that are sent with every request.
/// </summary>
public static class RestaurantTools
{
    public static List<ClaudeTool> All =>
    [
        new ClaudeTool
        {
            Name = "get_restaurant_info",
            Description =
                "Returns the restaurant's configuration: name, address, phone, email, " +
                "opening and closing hours, about section, and the full KnowledgeBase " +
                "which contains FAQs, policies, special services, holiday timings, payment methods, " +
                "parking info, Wi-Fi details, reservation policy, and any other restaurant-specific information. " +
                "Call this tool whenever a question requires any restaurant-specific information " +
                "that is not about the menu.",
            InputSchema = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject(),
                ["required"] = new JsonArray()
            }
        },

        new ClaudeTool
        {
            Name = "get_menu_catalog",
            Description =
                "Returns a lightweight list of ALL currently available menu items: " +
                "name, category, food type, and price. " +
                "Use this first to discover what items exist before fetching details.",
            InputSchema = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject(),
                ["required"] = new JsonArray()
            }
        },

        new ClaudeTool
        {
            Name = "get_menu_item_details",
            Description =
                "Returns full details for a specific menu item by name: " +
                "description, price, food type, ingredients (with approx quantity and unit), " +
                "and nutrition info (calories, protein, carbohydrates, fat, fiber, sugar, sodium). " +
                "Performs a fuzzy/partial name match so the user does not need to provide the exact name. " +
                "Use get_menu_catalog first if you are unsure which item to look up.",
            InputSchema = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["name"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "The menu item name or a partial name to search for."
                    }
                },
                ["required"] = new JsonArray { "name" }
            }
        },

        new ClaudeTool
        {
            Name = "get_menu_items_by_ingredient",
            Description =
                "Returns available menu items that contain — or do NOT contain — a given ingredient. " +
                "Useful for allergen queries ('what dishes contain peanuts?') or " +
                "dietary filters ('show me dishes without dairy'). " +
                "Set exclude=true to get the allergen-safe list.",
            InputSchema = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["ingredientName"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "The ingredient to search for (partial match, case-insensitive)."
                    },
                    ["exclude"] = new JsonObject
                    {
                        ["type"] = "boolean",
                        ["description"] =
                            "If true, returns items that do NOT contain the ingredient (allergen-safe). " +
                            "Default is false (returns items that DO contain it)."
                    }
                },
                ["required"] = new JsonArray { "ingredientName" }
            }
        }
    ];
}
