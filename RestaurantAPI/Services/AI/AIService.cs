namespace RestaurantAPI.Services.AI;

using RestaurantAPI.Services.AI.Contracts;
using RestaurantAPI.Services.AI.Contracts.Models;
public class AIService : IAIService
{
    private readonly IConversationService _conversationService;
    private readonly IClaudeClient _claudeClient;

    public AIService(
        IConversationService conversationService,
        IClaudeClient claudeClient)
    {
        _conversationService = conversationService;
        _claudeClient = claudeClient;
    }

    public async Task<ChatResponse> ChatAsync(ChatRequest request)
    {
        // Store the user's message
        await _conversationService.AddUserMessageAsync(
            request.ConversationId,
            request.Message);

        // Get the conversation history
        var messages = await _conversationService.GetMessagesAsync(
            request.ConversationId);

        // Send to Claude
        var response = await _claudeClient.SendAsync(
            SystemPrompt.Prompt,
            messages);

        // Store Claude's response
        await _conversationService.AddAssistantMessageAsync(
            request.ConversationId,
            response);

        // Return to caller
        return new ChatResponse
        {
            ConversationId = request.ConversationId,
            Response = response
        };
    }
}