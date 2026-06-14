using RestaurantAPI.Models.DTOs;

namespace RestaurantAPI.ServiceInterfaces;

public interface IAdminService
{
    Task<ICollection<OrderResponseDto>> GetTableOrders(int tableId);
    Task<BillResponseDto> GetTableBill(int tableId);
    Task<BillResponseDto> UpdateServiceCharge(int tableId, bool includeServiceCharge);
    Task CancelOrder(int tableId,int orderId);
    Task CancelOrderItem(int tableId,int orderId,int orderItemId);
    Task UpdateOrderItemQuantity(int tableId,int orderId,int orderItemId,int quantity);
    Task<WaiterManagementResponseDto> GetWaiters(string? search,bool? isActive);
    Task<TableResponseDto> AssignWaiter(int tableId, int waiterId);
    Task<TableResponseDto> RemoveWaiter(int tableId);
    Task UpdateWaiterStatus(int waiterId, bool isActive);
    Task DeleteWaiter(int waiterId);
}
