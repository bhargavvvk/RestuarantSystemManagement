using RestaurantAPI.Models.DTOs;

namespace RestaurantAPI.ServiceInterfaces;

public interface IDiningSessionService
{
    Task CloseSession(int waiterId, int tableId);
    Task<JoinSessionResponseDto> JoinSession(string qrIdentifier,JoinSessionRequestDto request);
    Task<CreateSessionResponseDto> CreateSession(string qrIdentifier,CreateSessionRequestDto request);
}
