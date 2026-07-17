using RestaurantAPI.Services.AI.Contracts;
using RestaurantAPI.Services.AI.Contracts.Models;

namespace RestaurantAPI.Services.AI.Conversation;
public class ConversationService : IConversationService
{
    private readonly Dictionary<string, List<ChatMessage>> _conversations = [];

    public Task<List<ChatMessage>> GetMessagesAsync(string conversationId)
    {
        if (!_conversations.TryGetValue(conversationId, out var messages))
        {
            messages = [];
            _conversations[conversationId] = messages;
        }

        return Task.FromResult(messages);
    }

    public async Task AddUserMessageAsync(string conversationId, string message)
    {
        var messages = await GetMessagesAsync(conversationId);

        messages.Add(new ChatMessage
        {
            Role = ChatRole.User,
            Content = message,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task AddAssistantMessageAsync(string conversationId, string message)
    {
        var messages = await GetMessagesAsync(conversationId);

        messages.Add(new ChatMessage
        {
            Role = ChatRole.Assistant,
            Content = message,
            Timestamp = DateTime.UtcNow
        });
    }

    public Task ClearConversationAsync(string conversationId)
    {
        _conversations.Remove(conversationId);

        return Task.CompletedTask;
    }
}