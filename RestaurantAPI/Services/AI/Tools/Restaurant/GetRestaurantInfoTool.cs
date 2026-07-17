using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;

namespace RestaurantAPI.Services.AI.Tools.Restaurant;

/// <summary>
/// Returns the restaurant's configuration — name, contact, hours, about,
/// and the full KnowledgeBase JSON (FAQs, policies, special services, etc.).
/// Claude should call this whenever any question requires restaurant-specific info.
/// </summary>
public class GetRestaurantInfoTool
{
    private readonly RestaurantContext _context;

    public GetRestaurantInfoTool(RestaurantContext context)
    {
        _context = context;
    }

    public async Task<string> ExecuteAsync()
    {
        var config = await _context.RestaurantConfigurations.FirstOrDefaultAsync();

        if (config == null)
            return JsonSerializer.Serialize(new
            {
                message = "Restaurant information has not been configured yet. " +
                          "Please ask a staff member for assistance."
            });

        // Parse KnowledgeBase from raw JSON string into an object so it
        // serialises naturally (not as an escaped string) in the tool result.
        object? knowledgeBase = null;
        if (!string.IsNullOrWhiteSpace(config.KnowledgeBase))
        {
            try
            {
                knowledgeBase = System.Text.Json.Nodes.JsonNode.Parse(config.KnowledgeBase);
            }
            catch
            {
                knowledgeBase = config.KnowledgeBase; // fallback: raw string
            }
        }

        var result = new
        {
            name = config.RestaurantName,
            description = config.Description,
            address = config.Address,
            phone = config.PhoneNumber,
            email = config.Email,
            openingTime = config.OpeningTime.HasValue
                ? config.OpeningTime.Value.ToString(@"hh\:mm") : null,
            closingTime = config.ClosingTime.HasValue
                ? config.ClosingTime.Value.ToString(@"hh\:mm") : null,
            about = config.About,
            knowledgeBase   // FAQs, policies, special services, etc.
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }
}
