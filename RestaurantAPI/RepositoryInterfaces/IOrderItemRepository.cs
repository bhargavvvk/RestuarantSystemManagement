using RestaurantAPI.Models;

namespace RestaurantAPI.RepositoryInterfaces;

public interface IOrderItemRepository:IRepository<int,OrderItem>
{
    Task<OrderItem?> GetOrderItemWithOrderAsync(int orderItemId);
    Task<ICollection<OrderItem>> GetActiveOrderItemsBySessionId(int sessionId);
}
