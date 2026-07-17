using RestaurantAPI.Services.AI.Contracts.Models;

namespace RestaurantAPI.Services.AI.Contracts;

public interface IConversationService
{
    Task<List<ChatMessage>> GetMessagesAsync(string conversationId);

    Task AddUserMessageAsync(string conversationId, string message);

    Task AddAssistantMessageAsync(string conversationId, string message);

    Task ClearConversationAsync(string conversationId);
}
