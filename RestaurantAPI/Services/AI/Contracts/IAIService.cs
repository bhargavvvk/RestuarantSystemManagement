using RestaurantAPI.Services.AI.Contracts.Models;

namespace RestaurantAPI.Services.AI.Contracts;

public interface IAIService
{
    Task<ChatResponse> ChatAsync(ChatRequest request);
}
