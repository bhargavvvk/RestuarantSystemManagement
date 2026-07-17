using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using RestaurantAPI.Services.AI.Configuration;
using RestaurantAPI.Services.AI.Contracts;
using RestaurantAPI.Services.AI.Contracts.Models;
using RestaurantAPI.Services.AI.Tools;

namespace RestaurantAPI.Services.AI.Claude;

public class ClaudeClient : IClaudeClient
{
    private readonly HttpClient _httpClient;
    private readonly AnthropicSettings _settings;
    private readonly IToolDispatcher _toolDispatcher;

    public ClaudeClient(
        HttpClient httpClient,
        IOptions<AnthropicSettings> options,
        IToolDispatcher toolDispatcher)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _toolDispatcher = toolDispatcher;
    }

    public async Task<string> SendAsync(
        string systemPrompt,
        List<ChatMessage> messages)
    {
        // Build the message list for Claude
        var claudeMessages = messages.Select(m => new ClaudeRequestMessage
        {
            Role = m.Role == ChatRole.User ? "user" : "assistant",
            Content = JsonValue.Create(m.Content)!
        }).ToList();

        // Agentic tool-use loop: keep calling Claude until stop_reason == "end_turn"
        const int maxIterations = 5;
        for (int i = 0; i < maxIterations; i++)
        {
            var request = new ClaudeRequest
            {
                Model = _settings.Model,
                MaxTokens = _settings.MaxTokens,
                System = systemPrompt,
                Messages = claudeMessages,
                Tools = RestaurantTools.All
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/messages");
            httpRequest.Headers.Add("x-api-key", _settings.ApiKey);
            httpRequest.Headers.Add("anthropic-version", "2023-06-01");
            httpRequest.Content = JsonContent.Create(request);

            var httpResponse = await _httpClient.SendAsync(httpRequest);
            httpResponse.EnsureSuccessStatusCode();

            var claudeResponse = await httpResponse.Content
                .ReadFromJsonAsync<ClaudeResponse>();

            if (claudeResponse == null)
                return "I'm sorry, I couldn't generate a response.";

            // ── end_turn: Claude is done — return the text ───────────────────
            if (claudeResponse.StopReason == "end_turn")
            {
                return claudeResponse.Content
                    .FirstOrDefault(b => b.Type == "text")?.Text
                    ?? "I'm sorry, I couldn't generate a response.";
            }

            // ── tool_use: Claude wants to call one or more tools ─────────────
            if (claudeResponse.StopReason == "tool_use")
            {
                // Append Claude's assistant turn (which contains tool_use blocks)
                var assistantContentArray = new JsonArray();
                foreach (var block in claudeResponse.Content)
                {
                    if (block.Type == "text" && !string.IsNullOrWhiteSpace(block.Text))
                    {
                        assistantContentArray.Add(new JsonObject
                        {
                            ["type"] = "text",
                            ["text"] = block.Text
                        });
                    }
                    else if (block.Type == "tool_use")
                    {
                        assistantContentArray.Add(new JsonObject
                        {
                            ["type"] = "tool_use",
                            ["id"] = block.Id,
                            ["name"] = block.Name,
                            ["input"] = block.Input?.DeepClone()
                        });
                    }
                }
                claudeMessages.Add(new ClaudeRequestMessage
                {
                    Role = "assistant",
                    Content = assistantContentArray
                });

                // Execute each tool call and collect results
                var toolResultsArray = new JsonArray();
                foreach (var block in claudeResponse.Content.Where(b => b.Type == "tool_use"))
                {
                    var toolResult = await _toolDispatcher.ExecuteAsync(
                        block.Name ?? string.Empty,
                        block.Input?.ToJsonString() ?? "{}");

                    toolResultsArray.Add(new JsonObject
                    {
                        ["type"] = "tool_result",
                        ["tool_use_id"] = block.Id,
                        ["content"] = toolResult
                    });
                }

                // Append the tool results as a user turn
                claudeMessages.Add(new ClaudeRequestMessage
                {
                    Role = "user",
                    Content = toolResultsArray
                });

                continue; // loop — send results back to Claude
            }

            // Any other stop reason — just return whatever text we have
            return claudeResponse.Content
                .FirstOrDefault(b => b.Type == "text")?.Text
                ?? "I'm sorry, I couldn't generate a response.";
        }

        return "I'm sorry, the response took too many steps to complete.";
    }
}
