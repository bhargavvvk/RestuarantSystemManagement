using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Services;

public class OrderService : IIOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderItemRepository _orderItemRepository;
    private readonly IBillRepository _billRepository;
    private readonly ICartItemRepository _cartItemRepository;
    private readonly IDiningSessionRepository _diningSessionRepository;
    private readonly ICartRepository _cartRepository;
    private readonly RestaurantContext _context;
    private readonly ILogger<OrderService> _logger;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IRestaurentTableRepository _restaurentTableRepository;
    private readonly IAuditService _auditService;
    private readonly IUserRepository _userRepository;
    private readonly IBillService _billService;
    public OrderService(IOrderRepository orderRepository, IOrderItemRepository orderItemRepository, IBillRepository billRepository, ICartItemRepository cartItemRepository, IDiningSessionRepository diningSessionRepository, RestaurantContext context, ILogger<OrderService> logger, ICartRepository cartRepository,
    IHubContext<NotificationHub> hubContext,IRestaurentTableRepository restaurentTableRepository,IAuditService auditService,IUserRepository userRepository,IBillService billService)
    {
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
        _billRepository = billRepository;
        _cartItemRepository = cartItemRepository;
        _diningSessionRepository = diningSessionRepository;
        _context = context;
        _logger = logger;
        _cartRepository = cartRepository;
        _hubContext = hubContext;
        _restaurentTableRepository = restaurentTableRepository;
        _auditService = auditService;
        _userRepository = userRepository;
        _billService=billService;
    }
    public async Task PlaceOrder(int sessionId,PlaceOrderRequestDto request)
    {
        _logger.LogInformation("Placing order for session");
        _logger.LogInformation($"{sessionId}");
        var session =await _diningSessionRepository.Get(sessionId);
        if(session == null)
        {
            throw new SessionNotFoundException();
        }
        _logger.LogInformation("session found");
        var cart = await _cartRepository.GetByDiningSessionId(sessionId);
        if(cart == null)
        {
            throw new CartNotFoundException();
        }
        _logger.LogInformation("cart found");
        _logger.LogInformation($"{cart}");
        _logger.LogInformation("Cart Id = {Id}", cart.Id);
        _logger.LogInformation("Before GetByCartId");
        var cartItems = await _cartItemRepository.GetByCartId(cart!.Id);
        _logger.LogInformation("After GetByCartId");
        _logger.LogInformation("Cart items found");
        if(!cartItems.Any())
        {
            throw new CartException("Cart is empty");
        }
        _logger.LogInformation("Checking if the menu items exists and available");
        foreach(var cartItem in cartItems)
        {
            if(cartItem.MenuItem == null)
            {
                throw new MenuItemNotFoundException();
            }

            if(!cartItem.MenuItem.IsAvailable)
            {
                throw new MenuItemUnavailableException();
            }
        }
        var bill = await _billRepository.GetBySessionId(sessionId);
        if(bill == null)
            {
                throw new BillNotFoundException();
            }
            if (bill.PaymentStatus == PaymentStatus.Paid)
            {
                throw new UnauthorizedAccessException(
                    "Cannot place orders for a paid bill.");
            }
        var orderTotal = cartItems.Sum(ci => ci.MenuItem!.Price * ci.Quantity);
        var orderNumber =await GenerateOrderNumber();
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _logger.LogInformation("Creating the order");
            var order =await _orderRepository.Create(
                new Order
                {
                    DiningSessionId = sessionId,
                    OrderNumber = orderNumber,
                    TotalAmount = orderTotal,
                    PlacedAt = DateTime.Now,
                    SpecialInstructions =
                    request.SpecialInstructions
                });
            await _orderRepository.SaveChangesAsync();
            foreach(var cartItem in cartItems)
            {
                await _orderItemRepository.Create( new OrderItem
                    {
                        OrderId = order.Id,
                        MenuItemId =cartItem.MenuItemId,
                        ItemName =cartItem.MenuItem!.Name,
                        ItemPrice =cartItem.MenuItem.Price,
                        Quantity =cartItem.Quantity,
                        Status =OrderItemStatus.Placed
                    });
            }
            await _orderItemRepository.SaveChangesAsync();
            bill.FoodTotal += orderTotal;
            var tax = bill.TaxConfiguration!;
            bill.CgstAmount = bill.FoodTotal * tax.CgstPercentage / 100;
            bill.SgstAmount = bill.FoodTotal * tax.SgstPercentage / 100;
            bill.ServiceChargeAmount = bill.FoodTotal * tax.ServiceChargePercentage / 100;
            bill.GrandTotal=bill.FoodTotal + bill.CgstAmount + bill.SgstAmount + bill.ServiceChargeAmount;
            await _billRepository.Update(bill.Id,bill);
            await _billRepository.SaveChangesAsync();
            foreach(var cartItem in cartItems)
            {
                await _cartItemRepository.Delete(cartItem.Id);
            }
            await _cartItemRepository.SaveChangesAsync();
            await transaction.CommitAsync();
            _logger.LogInformation("Creating the notification");
            _logger.LogInformation("Session Id: {Id}", session.Id);
            _logger.LogInformation("Table loaded: {Loaded}", session.Table != null);
            var table=await _restaurentTableRepository.Get(session.TableId);
            var notification =
            new OrderNotificationDto
            {
                TableNumber = table!.TableNumber,
                Message = "New order placed"
            };
            _logger.LogInformation("Created the notification");
            var kitchenId = await _userRepository.GetKitchenStaffId();
            await _hubContext.Clients.Group($"session-{sessionId}").SendAsync("OrderPlaced");
            await _hubContext.Clients.User(session.WaiterId.ToString()).SendAsync("ReceiveOrderPlaced", notification);
            await _hubContext.Clients.User(kitchenId.ToString()).SendAsync("OrderPlaced", notification);
            _logger.LogInformation("notification sent");
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    public async Task<ICollection<OrderResponseDto>> GetOrders(int sessionId)
    {
        _logger.LogInformation("Fetching orders for session {SessionId}", sessionId);
        var session =await _diningSessionRepository.Get(sessionId);
        if(session == null)
        {
            throw new SessionNotFoundException();
        }
        var orders = await _orderRepository.GetBySessionId(sessionId);
        return orders
        .Select(o => new OrderResponseDto
            {
            OrderId = o.Id,
            OrderNumber=o.OrderNumber,
            TotalAmount =o.TotalAmount,
                PlacedAt =o.PlacedAt,
                SpecialInstructions =o.SpecialInstructions,
                Items =
                    o.OrderItems!
                    .OrderBy(oi => oi.Status)
                    .Select(oi =>
                        new OrderItemResponseDto
                        {
                            OrderItemId=oi.Id,
                            ItemName =oi.ItemName,
                            ItemPrice =oi.ItemPrice,
                            Quantity =oi.Quantity,
                            Status =oi.Status
                        })
                    .ToList()
            })
        .ToList();
    }
     public async Task<int> GetTodayOrderCount()
    {
        return await _orderRepository.GetTodayOrderCount();
    }
    public async Task<KitchenOrdersResponseDto> GetKitchenOrders(OrderItemStatus status)
    {
        if (status != OrderItemStatus.Placed &&status != OrderItemStatus.Preparing &&status != OrderItemStatus.Ready)
        {
            throw new InvalidOperationException("Invalid kitchen status.");
        }
        var orders = await _orderRepository.GetKitchenOrders(status);

        var response = new KitchenOrdersResponseDto
        {
            QueueCount = await _orderRepository.GetKitchenQueueCount(),

            PreparingCount = await _orderRepository.GetKitchenPreparingCount(),

            ReadyCount = await _orderRepository.GetKitchenReadyCount(),

            Orders = orders.Select(order => new KitchenOrderDto
            {
                OrderId = order.Id,
                OrderNumber=order.OrderNumber,
                TableNumber = order.DiningSession!
                    .Table!
                    .TableNumber,

                PlacedAt = order.PlacedAt,

                Items = order.OrderItems!
                    .Select(item => new KitchenOrderItemDto
                    {
                        OrderItemId = item.Id,
                        ItemName = item.ItemName,
                        Quantity = item.Quantity,
                        Status = item.Status
                    })
                    .ToList()
            })
            .ToList()
        };

        return response;
    }
    public async Task CancelOrder(int orderId)
    {
        var order =await _orderRepository.GetOrderWithItems(orderId);
        if (order == null)
        {
            throw new OrderNotFoundException();
        }
        var session=order!.DiningSession;
        if (order.CancelledAt!=null)
        {
            throw new Exception("Order already cancelled");
        }
        if (order.OrderItems!.Any(oi => oi.Status != OrderItemStatus.Placed))
        {
            throw new Exception("Only orders in placed status can be cancelled");
        }
        var bill =await _billRepository.GetBySessionId(order.DiningSessionId);
        if (bill?.PaymentStatus == PaymentStatus.Paid)
        {
            throw new Exception("Cannot cancel order after payment");
        }
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            order.CancelledAt = DateTime.Now;
            foreach (var item in order.OrderItems!)
            {
                item.Status = OrderItemStatus.Cancelled;
            }
            await _orderRepository.SaveChangesAsync();
            await _billService.RecalculateBill(order.DiningSessionId);
            await _auditService.LogAsync(
                nameof(Order),
                order.Id.ToString(),
                AuditAction.Cancelled,
                remarks: $"Order {order.Id} cancelled");
            await _orderRepository.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
        await SendOrderCancelNotificationAsync(session!, order.OrderNumber, "Order Cancelled", "OrderCancelled");
    }
    public async Task CancelOrderItem(int orderId,int orderItemId)
    {
        var order = await _orderRepository.GetOrderWithItems(orderId);
        if (order == null)
        {
            throw new OrderNotFoundException();
        }
        if (order.CancelledAt != null)
        {
            throw new Exception("Order already cancelled");
        }
        var bill = await _billRepository.GetBySessionId(order.DiningSessionId);
        if (bill?.PaymentStatus == PaymentStatus.Paid)
        {
            throw new Exception("Cannot modify order after payment");
        }
        var item = order.OrderItems!.FirstOrDefault(oi => oi.Id == orderItemId);
        if (item == null)
        {
            throw new OrderItemNotFoundException();
        }

        if (order.OrderItems!.Any(oi => oi.Status != OrderItemStatus.Placed))
        {
            throw new Exception("Only orders in placed status can be modified");
        }
        if (order.OrderItems!.Count == 1)
        {
            await CancelOrder(orderId);
            return;
        }
        await using var transaction =await _context.Database.BeginTransactionAsync();
        try
        {
            order.CancelledAt = DateTime.Now;
            foreach (var orderItem in order.OrderItems)
            {
                orderItem.Status = OrderItemStatus.Cancelled;
            }
            var replacementItems = order.OrderItems.Where(oi => oi.Id != orderItemId)
                .Select(oldItem => new OrderItem
                {
                    MenuItemId = oldItem.MenuItemId,
                    ItemName = oldItem.ItemName,
                    ItemPrice = oldItem.ItemPrice,
                    Quantity = oldItem.Quantity,
                    Status = OrderItemStatus.Placed
                });

            var newOrder = await CreateReplacementOrder(order, replacementItems);
            await _orderRepository.SaveChangesAsync();
            await _billService.RecalculateBill(order.DiningSessionId);
            await _auditService.LogAsync(
                nameof(Order),
                order.Id.ToString(),
                AuditAction.Updated,
                remarks:
                $"Order item '{item.ItemName}' removed from order {order.Id}");

            await _orderRepository.SaveChangesAsync();

            await transaction.CommitAsync();

            var session = order.DiningSession;

            await SendOrderCancelNotificationAsync(session!, newOrder.OrderNumber, $"Item '{item.ItemName}' removed from order", "OrderModified");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<Order> CreateReplacementOrder(Order originalOrder,IEnumerable<OrderItem> replacementItems)
    {
        var orderNumber = await GenerateOrderNumber();
        var newOrder = new Order
        {
            DiningSessionId = originalOrder.DiningSessionId,
            OrderNumber = orderNumber,
            SpecialInstructions = originalOrder.SpecialInstructions,
            PlacedAt = DateTime.Now,
            OrderItems = replacementItems.ToList()
        };
        newOrder.TotalAmount = newOrder.OrderItems.Sum(oi => oi.ItemPrice * oi.Quantity);
        await _orderRepository.Create(newOrder);
        return newOrder;
    }
    public async Task UpdateOrderItemQuantity(int orderId,int orderItemId,int quantity)
    {
        var order =
            await _orderRepository.GetOrderWithItems(orderId);
        if (order == null)
        {
            throw new OrderNotFoundException();
        }

        if (order.CancelledAt != null)
        {
            throw new Exception("Order already cancelled");
        }

        var bill =await _billRepository.GetBySessionId(order.DiningSessionId);
        if (bill?.PaymentStatus == PaymentStatus.Paid)
        {
            throw new Exception("Cannot modify order after payment");
        }
        var item =order.OrderItems!.FirstOrDefault(oi => oi.Id == orderItemId);
        if (item == null)
        {
            throw new OrderItemNotFoundException();
        }

        if (order.OrderItems!.Any(oi => oi.Status != OrderItemStatus.Placed))
        {
            throw new Exception("Only orders in placed status can be modified");
        }
        if (quantity <= 0)
        {
            throw new Exception("Quantity must be greater than zero");
        }

        if (quantity >= item.Quantity)
        {
            throw new Exception("Updated quantity must be less than existing quantity");
        }
        await using var transaction =await _context.Database.BeginTransactionAsync();
        try
        {
            order.CancelledAt = DateTime.Now;
            foreach (var orderItem in order.OrderItems!)
            {
                orderItem.Status =OrderItemStatus.Cancelled;
            }
            var replacementItems =order.OrderItems
                    .Select(oi =>
                        new OrderItem
                        {
                            MenuItemId = oi.MenuItemId,
                            ItemName = oi.ItemName,
                            ItemPrice = oi.ItemPrice,
                            Quantity =
                                oi.Id == orderItemId
                                    ? quantity
                                    : oi.Quantity,
                            Status = OrderItemStatus.Placed
                        })
                    .ToList();

            var newOrder =await CreateReplacementOrder(order,replacementItems);
            await _orderRepository.SaveChangesAsync();
            await _billService.RecalculateBill(order.DiningSessionId);
            await _auditService.LogAsync(
                nameof(Order),
                order.Id.ToString(),
                AuditAction.Updated,
                remarks:
                $"Updated quantity of '{item.ItemName}' from {item.Quantity} to {quantity}");
            await _orderRepository.SaveChangesAsync();
            await transaction.CommitAsync();
            var session = order.DiningSession;
            var notification =
                new OrderCancelNotificationDto
                {
                    TableNumber =
                        session!.Table!.TableNumber,

                    OrderNumber = order.OrderNumber,

                    Message =
                        $"Quantity updated for {item.ItemName}"
                };

            var kitchenId =await _userRepository.GetKitchenStaffId();
            await _hubContext.Clients
                .User(session.WaiterId.ToString())
                .SendAsync(
                    "OrderModified",
                    notification);

            await _hubContext.Clients
                .User(kitchenId.ToString())
                .SendAsync(
                    "OrderModified",
                    notification);
            await _hubContext.Clients.Group($"session-{session.Id}").SendAsync("OrderModified", notification);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    public async Task<PagedResponseDto<OrderRegistryDto>>GetAllOrders(string search,DateOnly? date,int pageNumber,int pageSize)
    {
        var query = _orderRepository.GetOrdersQuery();
        if(pageNumber < 1)
        {
            pageNumber = 1;
        }
        if(date > DateOnly.FromDateTime(DateTime.Now))
        {
            throw new Exception("Future dates not allowed");
        }
        if(pageSize <= 0)
        {
            pageSize = 40;
        }

        pageSize = Math.Min(pageSize, 100);

        if (date.HasValue)
        {
            query = query.Where(o =>DateOnly.FromDateTime(o.PlacedAt)== date.Value);
        }

        query = query.OrderByDescending(o => o.PlacedAt);

        var totalCount = await query.CountAsync();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch =search.Trim();
            query = query.Where(o =>o.OrderNumber.Contains(normalizedSearch));
        }
        var orders = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderRegistryDto
            {
                OrderId = o.Id,
                OrderNumber=o.OrderNumber,
                TableNumber =o.DiningSession!.Table!.TableNumber,
                PlacedAt = o.PlacedAt,
                ItemCount =o.OrderItems!.Count,
                Status =
                    o.CancelledAt != null
                        ? "Cancelled"
                        : o.DiningSession.Status ==
                            DiningSessionStatus.Completed
                            ? "Completed"
                            : "Active"
            })
            .ToListAsync();
        return new PagedResponseDto<OrderRegistryDto>
        {
            Items = orders,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
    public async Task<OrderDetailsDto>GetOrderDetails(int orderId)
    {
        var order =await _orderRepository.GetOrderDetails(orderId);

        if (order == null)
        {
            throw new OrderNotFoundException();
        }

        var bill = order.DiningSession?.Bill;

        return new OrderDetailsDto
        {
            OrderId = order.Id,
            OrderNumber=order.OrderNumber,
            TableNumber =order.DiningSession!.Table!.TableNumber,
            PlacedAt = order.PlacedAt,
            Status =order.CancelledAt != null
                    ? "Cancelled"
                    : order.DiningSession.Status ==
                        DiningSessionStatus.Completed
                        ? "Completed"
                        : "Active",
            BillNumber =bill?.BillNumber ?? string.Empty,
            BillTotal =bill?.GrandTotal ?? 0,
            PaymentMethod = bill?.PaymentMethod,
            Items = order.OrderItems!
                .Select(oi =>
                    new OrderItemSummaryDto
                    {
                        ItemName = oi.ItemName,
                        Quantity = oi.Quantity
                    })
                .ToList()
        };
    }
    private async Task SendOrderCancelNotificationAsync(DiningSession session,string orderNumber,string message,string hubEvent)
    {
        var notification = new OrderCancelNotificationDto
        {
            TableNumber = session.Table!.TableNumber,
            OrderNumber = orderNumber,
            Message = message
        };
        var kitchenId = await _userRepository.GetKitchenStaffId();
        await _hubContext.Clients.Group($"session-{session.Id}").SendAsync(hubEvent,notification);
        await _hubContext.Clients.User(session.WaiterId.ToString()).SendAsync(hubEvent, notification);
        await _hubContext.Clients.User(kitchenId.ToString()).SendAsync(hubEvent, notification);
    }
    private async Task<string> GenerateOrderNumber()
    {
        var todayPart =DateTime.Today.ToString("yyyyMMdd");
        var latestOrderNumber =await _orderRepository.GetLatestOrderNumberToday();
        if (string.IsNullOrEmpty(latestOrderNumber))
        {
            return $"{todayPart}001";
        }
        var sequencePart =latestOrderNumber[^3..];
        var nextSequence =(int.Parse(sequencePart) + 1).ToString("D3");
        return $"{todayPart}{nextSequence}";
    }
}
