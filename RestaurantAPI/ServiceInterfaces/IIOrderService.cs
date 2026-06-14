using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;

namespace RestaurantAPI.ServiceInterfaces;

public interface IIOrderService
{
    Task PlaceOrder(int sessionId, PlaceOrderRequestDto request);
    Task<ICollection<OrderResponseDto>> GetOrders(int sessionId);
    Task<int> GetTodayOrderCount();
    Task<KitchenOrdersResponseDto> GetKitchenOrders(OrderItemStatus status);
    Task CancelOrder(int orderId);
    Task CancelOrderItem(int orderId,int orderItemId);
    Task UpdateOrderItemQuantity(int orderId,int orderItemId,int quantity);
    Task<PagedResponseDto<OrderRegistryDto>> GetAllOrders(string search,DateOnly? date, int pageNumber, int pageSize);
    Task<OrderDetailsDto> GetOrderDetails(int orderId);
}

