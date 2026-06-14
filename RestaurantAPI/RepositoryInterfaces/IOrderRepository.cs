using RestaurantAPI.Models;

namespace RestaurantAPI.RepositoryInterfaces;

public interface IOrderRepository:IRepository<int,Order>
{
    Task<ICollection<Order>> GetBySessionId(int sessionId);
    Task<IEnumerable<Order>> GetKitchenOrders(OrderItemStatus status);
    Task<int> GetKitchenQueueCount();

    Task<int> GetKitchenPreparingCount();

    Task<int> GetKitchenReadyCount();
    Task<int> GetTodayOrderCount();
    Task<Order?> GetOrderWithItems(int orderId);
    IQueryable<Order> GetOrdersQuery();
    Task<Order?> GetOrderDetails(int orderId);
    Task<string?> GetLatestOrderNumberToday();

}
