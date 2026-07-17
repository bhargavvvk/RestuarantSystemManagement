using RestaurantAPI.Models.DTOs;

namespace RestaurantAPI.ServiceInterfaces;

public interface IRestaurantConfigurationService
{
    Task<RestaurantConfigurationResponseDto> GetConfiguration();
    Task<RestaurantConfigurationResponseDto> UpsertDetails(UpdateRestaurantDetailsDto request);
    Task<RestaurantConfigurationResponseDto> UpdateKnowledgeBase(UpdateKnowledgeBaseDto request);
    Task<object?> GetKnowledgeBase();
}
