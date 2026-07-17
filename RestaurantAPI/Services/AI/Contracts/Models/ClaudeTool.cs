using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace RestaurantAPI.Services.AI.Contracts.Models;

/// <summary>A tool definition sent to Claude in the request.</summary>
public class ClaudeTool
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("input_schema")]
    public JsonObject InputSchema { get; set; } = [];
}

/// <summary>A tool_use block returned by Claude when it wants to call a tool.</summary>
public class ClaudeToolUseBlock
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;   // "tool_use"

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("input")]
    public JsonObject Input { get; set; } = [];
}

/// <summary>
/// A tool_result block we send back to Claude after executing the tool.
/// This is a message content block with role=user.
/// </summary>
public class ClaudeToolResultBlock
{
    [JsonPropertyName("type")]
    public string Type { get; } = "tool_result";

    [JsonPropertyName("tool_use_id")]
    public string ToolUseId { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}
