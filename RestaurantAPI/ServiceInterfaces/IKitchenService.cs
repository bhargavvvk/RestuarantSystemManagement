namespace RestaurantAPI.ServiceInterfaces;

public interface IKitchenService
{
    Task MarkOrderItemReady(int orderId, int orderItemId);
    Task StartPreparing(int orderId);
}
