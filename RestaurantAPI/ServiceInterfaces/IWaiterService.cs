using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;

namespace RestaurantAPI.ServiceInterfaces;

public interface IWaiterService
{
    Task<ICollection<CartItemResponseDto>> GetTableCart(int waiterId, int tableId);
    Task AddItemToTableCart(int waiterId, int tableId, AddToCartDto request);
    Task UpdateTableCartItem(int waiterId, int tableId, int cartItemId, UpdateCartItemDto request);
    Task RemoveTableCartItem(int waiterId, int tableId, int cartItemId);
    Task<ICollection<OrderResponseDto>> GetTableOrders(int waiterId, int tableId);
    Task PlaceOrder(int waiterId, int tableId, PlaceOrderRequestDto request);
    Task<BillResponseDto> GetTableBill(int waiterId, int tableId);
    Task<BillResponseDto> MarkTableBillAsPaid(int waiterId, int tableId, MarkBillPaidDto request);
    Task<OrderItemResponseDto> MarkOrderItemAsServed(int waiterId, int tableId, int orderItemId);
}