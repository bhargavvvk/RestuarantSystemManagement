using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace RestaurantAPI.Services.AI.Contracts.Models;

/// <summary>Top-level response envelope from the Claude Messages API.</summary>
public class ClaudeResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("stop_reason")]
    public string StopReason { get; set; } = string.Empty;  // "end_turn" | "tool_use"

    [JsonPropertyName("content")]
    public List<ClaudeContentBlock> Content { get; set; } = [];
}

/// <summary>
/// A single content block. Type is "text" or "tool_use".
/// </summary>
public class ClaudeContentBlock
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    // ── text block ────────────────────────────────────────────────────────────
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    // ── tool_use block ────────────────────────────────────────────────────────
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("input")]
    public JsonObject? Input { get; set; }
}
