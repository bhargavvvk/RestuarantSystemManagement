namespace RestaurantAPI.Services.AI.Configuration;

public class AnthropicSettings
{
    public const string SectionName = "Anthropic";

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public int MaxTokens { get; set; }

    public double Temperature { get; set; }
}