using RestaurantAPI.Models.DTOs;

namespace RestaurantAPI.ServiceInterfaces;

public interface ICustomerRequestService
{
    Task CompleteRequest(int waiterId, int requestId);
    Task<ICollection<CustomerRequestResponseDto>> GetActiveRequests(int waiterId);
     Task<string> CreateRequest(int sessionId,CreateCustomerRequestDto request);
}
