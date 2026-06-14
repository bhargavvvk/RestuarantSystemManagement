using RestaurantAPI.Models;

namespace RestaurantAPI.RepositoryInterfaces;

public interface ICartRepository:IRepository<int,Cart>
{
    Task<Cart?> GetByDiningSessionId(int diningSessionId);
}
