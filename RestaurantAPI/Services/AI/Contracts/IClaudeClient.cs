using RestaurantAPI.Services.AI.Contracts.Models;

namespace RestaurantAPI.Services.AI.Contracts;

public interface IClaudeClient
{
    Task<string> SendAsync(string systemPrompt,List<ChatMessage> messages);
}
