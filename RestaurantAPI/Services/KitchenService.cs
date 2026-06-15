using Microsoft.AspNetCore.SignalR;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Services;

public class KitchenService:IKitchenService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<KitchenService> _logger;
    private readonly IHubContext<NotificationHub> _hubContext;
    public KitchenService(IOrderRepository orderRepository,ILogger<KitchenService> logger,IHubContext<NotificationHub> hubContext)
    {
        _orderRepository = orderRepository;
        _logger = logger;
        _hubContext = hubContext;
    }
    public async Task MarkOrderItemReady(int orderId,int orderItemId)
    {
        var order = await _orderRepository.GetOrderWithItems(orderId);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found", orderId);
            throw new OrderNotFoundException();
        }
        if (order.CancelledAt != null)
        {
            throw new Exception("Order has been cancelled");
        }
        var orderItem = order.OrderItems?.FirstOrDefault(i => i.Id == orderItemId);
        if (orderItem == null)
        {
            throw new OrderItemNotFoundException("Order item not found in the specified order");
        }
        if (orderItem.Status != OrderItemStatus.Preparing)
        {
            throw new Exception("Only preparing items can be marked as ready");
        }
        orderItem.Status = OrderItemStatus.Ready;
        await _orderRepository.SaveChangesAsync();
        var waiterId = order.DiningSession!.WaiterId.ToString();
        await _hubContext.Clients.User(waiterId).SendAsync("OrderItemReady",
                new {
                    OrderId = order.Id,
                    TableNumber = order
                        .DiningSession!
                        .Table!
                        .TableNumber,
                    ItemName= orderItem.ItemName
                });

        _logger.LogInformation("Order item {OrderItemId} marked ready for waiter {WaiterId}",orderItemId,waiterId);
    }
    public async Task StartPreparing(int orderId)
    {
        _logger.LogInformation("Starting preparation for order {OrderId}", orderId);
        var order = await _orderRepository.GetOrderWithItems(orderId);
        if (order == null) throw new OrderNotFoundException();

        if (order.CancelledAt != null)  throw new Exception("Order is cancelled.");

        if (!order.OrderItems!.All(i =>i.Status == OrderItemStatus.Placed))
        {
            throw new Exception("All order items must be in Placed status.");
        }
        foreach (var item in order.OrderItems!)
        {
            item.Status = OrderItemStatus.Preparing;
        }

        await _orderRepository.SaveChangesAsync();
        _logger.LogInformation("Order {OrderId} is now being prepared", orderId);
    }
}
