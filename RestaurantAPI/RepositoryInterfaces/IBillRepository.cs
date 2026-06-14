using RestaurantAPI.Models;

namespace RestaurantAPI.RepositoryInterfaces;

public interface IBillRepository:IRepository<int,Bill>
{
    Task<int> GetBillCount();
    Task<Bill?> GetBySessionId(int sessionId);
    IQueryable<Bill> GetBillsQuery();
    Task<Bill?> GetBillDetails(int billId);
    Task<string?> GetLatestBillNumberToday();
}
