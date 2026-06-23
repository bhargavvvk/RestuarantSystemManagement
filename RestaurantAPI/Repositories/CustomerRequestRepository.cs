using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;

namespace RestaurantAPI.Repositories;

public class CustomerRequestRepository:AbstractRepository<int,CustomerRequest>,ICustomerRequestRepository
{
    public CustomerRequestRepository(RestaurantContext context):base(context)
    {

    }

    public async Task<ICollection<CustomerRequest>> GetActiveRequestsByWaiterId(int waiterId)
    {
        return await _context.CustomerRequests
                            .Include(cr => cr.DiningSession)
                                .ThenInclude(ds => ds.Table)
                            .Where(cr =>
                                cr.Status == CustomerRequestStatus.Pending &&
                                cr.DiningSession.WaiterId == waiterId &&
                                cr.DiningSession.Status == DiningSessionStatus.Active)
                            .OrderBy(cr => cr.RequestedAt)
                            .ToListAsync();

    }

    public async Task<CustomerRequest?> GetPendingRequest(int diningSessionId, CustomerRequestType requestType)
    {
        return await _context.CustomerRequests.FirstOrDefaultAsync(cr => cr.DiningSessionId == diningSessionId && cr.RequestType == requestType && cr.Status==CustomerRequestStatus.Pending);
    }

    public async Task<CustomerRequest?> GetRequestWithSession(int requestId)
    {
       return await _context.CustomerRequests
                    .Include(cr => cr.DiningSession)
                    .FirstOrDefaultAsync(cr =>
                        cr.Id == requestId);
    }
}
