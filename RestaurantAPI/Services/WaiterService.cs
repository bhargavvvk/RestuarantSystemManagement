using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using RestaurantAPI.Contexts;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Services;

public class WaiterService:IWaiterService
{
    readonly ICartService _cartService;
    readonly IDiningSessionRepository _diningSessionRepository;
    readonly ILogger<IWaiterService> _logger;
    readonly IIOrderService _orderService;
    private readonly IBillService _billService;
    private readonly IOrderItemRepository _orderItemRepository;
    private readonly IMapper _mapper;
    private readonly IHubContext<NotificationHub> _hubContext;
    public WaiterService(ICartService cartService, IDiningSessionRepository diningSessionRepository, ILogger<IWaiterService> logger, IIOrderService orderService,
    IBillService billService,IOrderItemRepository orderItemRepository, IMapper mapper,IHubContext<NotificationHub> hubContext)
    {
        _cartService = cartService;
        _diningSessionRepository = diningSessionRepository;
        _logger = logger;
        _orderService = orderService;
        _billService = billService;
        _orderItemRepository = orderItemRepository;
        _mapper = mapper;
        _hubContext=hubContext;
    }

    public async Task<ICollection<CartItemResponseDto>> GetTableCart(int waiterId, int tableId)
    {
        _logger.LogInformation("Retriving cart for table {TableId}", tableId);
        var session = await _diningSessionRepository.GetActiveSessionWithCartByTableId(tableId);

        if (session == null)
        {
            throw new SessionNotFoundException();
        }

        if (session.WaiterId != waiterId)
        {
            throw new UnauthorizedAccessException("Table is not assigned to the logged-in waiter.");
        }
        return await _cartService.GetCartItems(session.Cart!.Id);
    }
    public async Task AddItemToTableCart(int waiterId, int tableId, AddToCartDto request)
    {
        _logger.LogInformation("Waiter {WaiterId} adding menu item {MenuItemId} to cart for table {TableId}", waiterId, request.MenuItemId, tableId);
         var session = await _diningSessionRepository.GetActiveSessionWithCartByTableId(tableId);

        if (session == null)
            throw new SessionNotFoundException();

        if (session.WaiterId != waiterId)
            throw new UnauthorizedAccessException( "Table is not assigned to the logged-in waiter.");

        await _cartService.AddToCart(session.Id,session.Cart!.Id,request);
    }
    public async Task UpdateTableCartItem(int waiterId, int tableId, int cartItemId, UpdateCartItemDto request)
    {
        _logger.LogInformation("Waiter {WaiterId} updating cart item {CartItemId} for table {TableId}", waiterId, cartItemId, tableId);
        var session = await _diningSessionRepository.GetActiveSessionWithCartByTableId(tableId);
            if (session == null)
            {
                throw new SessionNotFoundException();
            }

            if (session.WaiterId != waiterId)
            {
                throw new UnauthorizedAccessException("Table is not assigned to the logged-in waiter.");
            }

            await _cartService.UpdateCartItem(session.Id,
                session.Cart!.Id,
                cartItemId,
                request);
    }
    public async Task RemoveTableCartItem(int waiterId, int tableId, int cartItemId)
    {
        _logger.LogInformation("Waiter {WaiterId} removing cart item {CartItemId} for table {TableId}", waiterId, cartItemId, tableId);
         var session = await _diningSessionRepository.GetActiveSessionWithCartByTableId(tableId);

        if (session == null)
        {
            throw new SessionNotFoundException();
        }

        if (session.WaiterId != waiterId)
        {
            throw new UnauthorizedAccessException("Table is not assigned to the logged-in waiter.");
        }

        await _cartService.RemoveCartItem(session.Id,session.Cart!.Id,cartItemId);
    }
    public async Task<ICollection<OrderResponseDto>> GetTableOrders(int waiterId, int tableId)
    {
        _logger.LogInformation("Waiter {WaiterId} retrieving orders for table {TableId}", waiterId, tableId);
        var session = await _diningSessionRepository.GetActiveSessionByTableId(tableId);
        if (session == null)
        {
            throw new SessionNotFoundException();
        }
        if (session.WaiterId != waiterId)
        {
            throw new UnauthorizedAccessException("Table is not assigned to the logged-in waiter.");
        }
        return await _orderService.GetOrders(session.Id);
    }
    public async Task PlaceOrder(int waiterId, int tableId, PlaceOrderRequestDto request)
    {
        _logger.LogInformation("Waiter {WaiterId} placing order for table {TableId}", waiterId, tableId);
        var session = await _diningSessionRepository.GetActiveSessionWithCartByTableId(tableId);
        if (session == null)
            {
                throw new SessionNotFoundException();
            }

            if (session.WaiterId != waiterId)
            {
                throw new UnauthorizedAccessException("Table is not assigned to the logged-in waiter.");
        }
        await _orderService.PlaceOrder(session.Id,request);
    }
    public async Task<BillResponseDto> GetTableBill(int waiterId,int tableId)
    {
        _logger.LogInformation("Waiter {WaiterId} retrieving bill for table {TableId}", waiterId, tableId);
        var session = await _diningSessionRepository.GetActiveSessionByTableId(tableId);
        if (session == null)
        {
            throw new SessionNotFoundException();
        }

        if (session.WaiterId != waiterId)
        {
            throw new UnauthorizedAccessException("Table is not assigned to the logged-in waiter.");
        }
        return await _billService.GetBill(session.Id);
    }
    public async Task<BillResponseDto> MarkTableBillAsPaid(int waiterId, int tableId, MarkBillPaidDto request)
    {
        _logger.LogInformation("Waiter {WaiterId} marking bill as paid for table {TableId}", waiterId, tableId);
        if (!Enum.IsDefined(typeof(PaymentMethod),request.PaymentMethod))
        {
            throw new ValidationException("Invalid payment method.");
        }
        var session = await _diningSessionRepository.GetActiveSessionByTableId(tableId);

        if (session == null)
        {
            throw new SessionNotFoundException();
        }

        if (session.WaiterId != waiterId)
        {
            throw new UnauthorizedAccessException("Table is not assigned to the logged-in waiter.");
        }

        return await _billService.MarkBillAsPaid(session.Id,request.PaymentMethod);
    }
    public async Task<OrderItemResponseDto> MarkOrderItemAsServed(int waiterId, int tableId, int orderItemId)
    {
        var session = await _diningSessionRepository.GetActiveSessionByTableId(tableId);
        if (session == null)
        {
            throw new SessionNotFoundException();
        }
        if (session.WaiterId != waiterId)
        {
            throw new UnauthorizedAccessException("Table is not assigned to the logged-in waiter.");
        }

        var orderItem = await _orderItemRepository.GetOrderItemWithOrderAsync(orderItemId);

        if (orderItem == null)
        {
            throw new OrderItemNotFoundException();
        }

        if (orderItem.Order!.DiningSessionId != session.Id)
        {
            throw new UnauthorizedAccessException("Order item does not belong to this table.");
        }
        if (orderItem.Status == OrderItemStatus.Served)
        {
            throw new ValidationException("Item is already Served");
        }
        if (orderItem.Status != OrderItemStatus.Ready)
        {
            throw new ValidationException("Only ready items can be marked as served.");
        }

        orderItem.Status = OrderItemStatus.Served;
        await _orderItemRepository.Update(orderItem.Id, orderItem);
        await _orderItemRepository.SaveChangesAsync();
        await _hubContext.Clients.Group($"session-{session.Id}").SendAsync("ItemServed", $"{orderItem.ItemName} is served");
        await _hubContext.Clients.Group("kitchen").SendAsync("ItemServed", $"{orderItem.ItemName} is served");
        _logger.LogInformation("Order item {OrderItemId} marked as served by waiter {WaiterId} for table {TableId}", orderItemId, waiterId, tableId);
        return _mapper.Map<OrderItemResponseDto>(orderItem);
    }
}
