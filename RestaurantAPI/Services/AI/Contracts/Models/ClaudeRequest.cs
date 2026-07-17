using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace RestaurantAPI.Services.AI.Contracts.Models;

public class ClaudeRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }

    [JsonPropertyName("system")]
    public string System { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<ClaudeRequestMessage> Messages { get; set; } = [];

    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ClaudeTool>? Tools { get; set; }
}

/// <summary>
/// A message in the Claude request. Content can be:
/// - A plain string (user/assistant text turns)
/// - A list of content blocks (tool_use or tool_result turns)
/// </summary>
public class ClaudeRequestMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Either a string or a List of content blocks.
    /// Using JsonNode so both forms serialize correctly.
    /// </summary>
    [JsonPropertyName("content")]
    public JsonNode Content { get; set; } = JsonValue.Create(string.Empty)!;
}
