using RestaurantAPI.Models;

namespace RestaurantAPI.RepositoryInterfaces;

public interface ICustomerRequestRepository:IRepository<int,CustomerRequest>
{
    Task<CustomerRequest?> GetPendingRequest(int diningSessionId, CustomerRequestType requestType);
    Task<ICollection<CustomerRequest>> GetActiveRequestsByWaiterId(int waiterId);
    Task<CustomerRequest?> GetRequestWithSession(int requestId);
}
