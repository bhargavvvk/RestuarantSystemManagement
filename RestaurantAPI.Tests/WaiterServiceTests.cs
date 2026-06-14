using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;
using RestaurantAPI.Services;

namespace RestaurantAPI.Tests;

public class WaiterServiceTests
{
    private Mock<IDiningSessionRepository>    _sessionRepoMock;
    private Mock<IOrderItemRepository>        _orderItemRepoMock;
    private Mock<IMapper>                     _mapperMock;
    private Mock<IHubContext<NotificationHub>> _hubMock;
    private Mock<ICartService>                _cartServiceMock;
    private Mock<IIOrderService>              _orderServiceMock;
    private Mock<IBillService>                _billServiceMock;
    private WaiterService                     _waiterService;

    private const int WaiterId    = 1;
    private const int TableId     = 10;
    private const int SessionId   = 20;
    private const int OrderItemId = 30;
    private const int CartId      = 40;
    private const int CartItemId  = 50;

    [SetUp]
    public void SetUp()
    {
        _sessionRepoMock   = new Mock<IDiningSessionRepository>();
        _orderItemRepoMock = new Mock<IOrderItemRepository>();
        _mapperMock        = new Mock<IMapper>();
        _cartServiceMock   = new Mock<ICartService>();
        _orderServiceMock  = new Mock<IIOrderService>();
        _billServiceMock   = new Mock<IBillService>();

        var clientsMock = new Mock<IHubClients>();
        var proxyMock   = new Mock<IClientProxy>();
        proxyMock
            .Setup(p => p.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        clientsMock.Setup(c => c.User(It.IsAny<string>())).Returns(proxyMock.Object);
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(proxyMock.Object);

        _hubMock = new Mock<IHubContext<NotificationHub>>();
        _hubMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        _orderItemRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);
        _orderItemRepoMock
            .Setup(r => r.Update(It.IsAny<int>(), It.IsAny<OrderItem>()))
            .ReturnsAsync((int _, OrderItem i) => i);

        _waiterService = new WaiterService(
            _cartServiceMock.Object,
            _sessionRepoMock.Object,
            NullLogger<IWaiterService>.Instance,
            _orderServiceMock.Object,
            _billServiceMock.Object,
            _orderItemRepoMock.Object,
            _mapperMock.Object,
            _hubMock.Object);
    }



    private static DiningSession ActiveSession() => new()
    {
        Id       = SessionId,
        WaiterId = WaiterId,
        Cart     = new Cart { Id = CartId },
        Status   = DiningSessionStatus.Active
    };

    private static OrderItem ReadyItem() => new()
    {
        Id       = OrderItemId,
        ItemName = "Burger",
        Status   = OrderItemStatus.Ready,
        Order    = new Order { DiningSessionId = SessionId }
    };

   

    [Test]
    public async Task GetTableCart_Success()
    {
        var session   = ActiveSession();
        var cartItems = new List<CartItemResponseDto> { new() { Id = CartItemId } };

        _sessionRepoMock.Setup(r => r.GetActiveSessionWithCartByTableId(TableId)).ReturnsAsync(session);
        _cartServiceMock.Setup(s => s.GetCartItems(CartId)).ReturnsAsync(cartItems);

        var result = await _waiterService.GetTableCart(WaiterId, TableId);

        Assert.That(result, Has.Count.EqualTo(1));
        _cartServiceMock.Verify(s => s.GetCartItems(CartId), Times.Once);
    }

    [Test]
    public void GetTableCart_SessionNotFound()
    {
        _sessionRepoMock.Setup(r => r.GetActiveSessionWithCartByTableId(TableId))
                        .ReturnsAsync((DiningSession?)null);

        Assert.ThrowsAsync<SessionNotFoundException>(
            () => _waiterService.GetTableCart(WaiterId, TableId));
    }

    [Test]
    public void GetTableCart_UnauthorizedWaiter()
    {
        var session = ActiveSession();
        session.WaiterId = 999;

        _sessionRepoMock.Setup(r => r.GetActiveSessionWithCartByTableId(TableId)).ReturnsAsync(session);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _waiterService.GetTableCart(WaiterId, TableId));
    }

  

    [Test]
    public async Task AddItemToTableCart_Success()
    {
        var session = ActiveSession();
        var request = new AddToCartDto { MenuItemId = 99 };

        _sessionRepoMock.Setup(r => r.GetActiveSessionWithCartByTableId(TableId)).ReturnsAsync(session);
        _cartServiceMock.Setup(s => s.AddToCart(CartId, request)).Returns(Task.CompletedTask);

        await _waiterService.AddItemToTableCart(WaiterId, TableId, request);

        _cartServiceMock.Verify(s => s.AddToCart(CartId, request), Times.Once);
    }

    [Test]
    public void AddItemToTableCart_SessionNotFound()
    {
        _sessionRepoMock.Setup(r => r.GetActiveSessionWithCartByTableId(TableId))
                        .ReturnsAsync((DiningSession?)null);

        Assert.ThrowsAsync<SessionNotFoundException>(
            () => _waiterService.AddItemToTableCart(WaiterId, TableId, new AddToCartDto()));
    }

    [Test]
    public void AddItemToTableCart_UnauthorizedWaiter()
    {
        var session = ActiveSession();
        session.WaiterId = 999;

        _sessionRepoMock.Setup(r => r.GetActiveSessionWithCartByTableId(TableId)).ReturnsAsync(session);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _waiterService.AddItemToTableCart(WaiterId, TableId, new AddToCartDto()));
    }



    [Test]
    public async Task UpdateTableCartItem_Success()
    {
        var session = ActiveSession();
        var request = new UpdateCartItemDto { Quantity = 3 };

        _sessionRepoMock.Setup(r => r.GetActiveSessionWithCartByTableId(TableId)).ReturnsAsync(session);
        _cartServiceMock.Setup(s => s.UpdateCartItem(CartId, CartItemId, request)).Returns(Task.CompletedTask);

        await _waiterService.UpdateTableCartItem(WaiterId, TableId, CartItemId, request);

        _cartServiceMock.Verify(s => s.UpdateCartItem(CartId, CartItemId, request), Times.Once);
    }

    [Test]
    public void UpdateTableCartItem_SessionNotFound()
    {
        _sessionRepoMock.Setup(r => r.GetActiveSessionWithCartByTableId(TableId))
                        .ReturnsAsync((DiningSession?)null);

        Assert.ThrowsAsync<SessionNotFoundException>(
            () => _waiterService.UpdateTableCartItem(WaiterId, TableId, CartItemId, new UpdateCartItemDto()));
    }

    [Test]
    public void UpdateTableCartItem_UnauthorizedWaiter()
    {
        var session = ActiveSession();
        session.WaiterId = 999;

        _sessionRepoMock.Setup(r => r.GetActiveSessionWithCartByTableId(TableId)).ReturnsAsync(session);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _waiterService.UpdateTableCartItem(WaiterId, TableId, CartItemId, new UpdateCartItemDto()));
    }



    [Test]
    public async Task RemoveTableCartItem_Success()
    {
        var session = ActiveSession();

        _sessionRepoMock.Setup(r => r.GetActiveSessionWithCartByTableId(TableId)).ReturnsAsync(session);
        _cartServiceMock.Setup(s => s.RemoveCartItem(CartId, CartItemId)).Returns(Task.CompletedTask);

        await _waiterService.RemoveTableCartItem(WaiterId, TableId, CartItemId);

        _cartServiceMock.Verify(s => s.RemoveCartItem(CartId, CartItemId), Times.Once);
    }

    [Test]
    public void RemoveTableCartItem_SessionNotFound()
    {
        _sessionRepoMock.Setup(r => r.GetActiveSessionWithCartByTableId(TableId))
                        .ReturnsAsync((DiningSession?)null);

        Assert.ThrowsAsync<SessionNotFoundException>(
            () => _waiterService.RemoveTableCartItem(WaiterId, TableId, CartItemId));
    }

    [Test]
    public void RemoveTableCartItem_UnauthorizedWaiter()
    {
        var session = ActiveSession();
        session.WaiterId = 999;

        _sessionRepoMock.Setup(r => r.GetActiveSessionWithCartByTableId(TableId)).ReturnsAsync(session);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _waiterService.RemoveTableCartItem(WaiterId, TableId, CartItemId));
    }

  
    [Test]
    public async Task GetTableOrders_Success()
    {
        var session = ActiveSession();
        var orders  = new List<OrderResponseDto> { new() { OrderId = 1 } };

        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId)).ReturnsAsync(session);
        _orderServiceMock.Setup(s => s.GetOrders(SessionId)).ReturnsAsync(orders);

        var result = await _waiterService.GetTableOrders(WaiterId, TableId);

        Assert.That(result, Has.Count.EqualTo(1));
        _orderServiceMock.Verify(s => s.GetOrders(SessionId), Times.Once);
    }

    [Test]
    public void GetTableOrders_SessionNotFound()
    {
        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId))
                        .ReturnsAsync((DiningSession?)null);

        Assert.ThrowsAsync<SessionNotFoundException>(
            () => _waiterService.GetTableOrders(WaiterId, TableId));
    }

    [Test]
    public void GetTableOrders_UnauthorizedWaiter()
    {
        var session = ActiveSession();
        session.WaiterId = 999;

        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId)).ReturnsAsync(session);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _waiterService.GetTableOrders(WaiterId, TableId));
    }

   

    [Test]
    public async Task PlaceOrder_Success()
    {
        var session = ActiveSession();
        var request = new PlaceOrderRequestDto { SpecialInstructions = "No onions" };

        _sessionRepoMock.Setup(r => r.GetActiveSessionWithCartByTableId(TableId)).ReturnsAsync(session);
        _orderServiceMock.Setup(s => s.PlaceOrder(CartId, request)).Returns(Task.CompletedTask);

        await _waiterService.PlaceOrder(WaiterId, TableId, request);

        _orderServiceMock.Verify(s => s.PlaceOrder(CartId, request), Times.Once);
    }

    [Test]
    public void PlaceOrder_SessionNotFound()
    {
        _sessionRepoMock.Setup(r => r.GetActiveSessionWithCartByTableId(TableId))
                        .ReturnsAsync((DiningSession?)null);

        Assert.ThrowsAsync<SessionNotFoundException>(
            () => _waiterService.PlaceOrder(WaiterId, TableId, new PlaceOrderRequestDto()));
    }

    [Test]
    public void PlaceOrder_UnauthorizedWaiter()
    {
        var session = ActiveSession();
        session.WaiterId = 999;

        _sessionRepoMock.Setup(r => r.GetActiveSessionWithCartByTableId(TableId)).ReturnsAsync(session);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _waiterService.PlaceOrder(WaiterId, TableId, new PlaceOrderRequestDto()));
    }

    // ─── GetTableBill ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetTableBill_Success()
    {
        var session      = ActiveSession();
        var billResponse = new BillResponseDto { BillNumber = "BILL-001" };

        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId)).ReturnsAsync(session);
        _billServiceMock.Setup(s => s.GetBill(SessionId)).ReturnsAsync(billResponse);

        var result = await _waiterService.GetTableBill(WaiterId, TableId);

        Assert.That(result.BillNumber, Is.EqualTo("BILL-001"));
        _billServiceMock.Verify(s => s.GetBill(SessionId), Times.Once);
    }

    [Test]
    public void GetTableBill_SessionNotFound()
    {
        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId))
                        .ReturnsAsync((DiningSession?)null);

        Assert.ThrowsAsync<SessionNotFoundException>(
            () => _waiterService.GetTableBill(WaiterId, TableId));
    }

    [Test]
    public void GetTableBill_UnauthorizedWaiter()
    {
        var session = ActiveSession();
        session.WaiterId = 999;

        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId)).ReturnsAsync(session);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _waiterService.GetTableBill(WaiterId, TableId));
    }

   

    [Test]
    public async Task MarkTableBillAsPaid_Success()
    {
        var session      = ActiveSession();
        var request      = new MarkBillPaidDto { PaymentMethod = PaymentMethod.Cash };
        var billResponse = new BillResponseDto { BillNumber = "BILL-001" };

        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId)).ReturnsAsync(session);
        _billServiceMock.Setup(s => s.MarkBillAsPaid(SessionId, PaymentMethod.Cash)).ReturnsAsync(billResponse);

        var result = await _waiterService.MarkTableBillAsPaid(WaiterId, TableId, request);

        Assert.That(result.BillNumber, Is.EqualTo("BILL-001"));
        _billServiceMock.Verify(s => s.MarkBillAsPaid(SessionId, PaymentMethod.Cash), Times.Once);
    }

    [Test]
    public void MarkTableBillAsPaid_InvalidPaymentMethod()
    {
        var request = new MarkBillPaidDto { PaymentMethod = (PaymentMethod)999 };

        var ex = Assert.ThrowsAsync<ValidationException>(
            () => _waiterService.MarkTableBillAsPaid(WaiterId, TableId, request));

        Assert.That(ex!.Message, Is.EqualTo("Invalid payment method."));
    }

    [Test]
    public void MarkTableBillAsPaid_SessionNotFound()
    {
        var request = new MarkBillPaidDto { PaymentMethod = PaymentMethod.Cash };

        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId))
                        .ReturnsAsync((DiningSession?)null);

        Assert.ThrowsAsync<SessionNotFoundException>(
            () => _waiterService.MarkTableBillAsPaid(WaiterId, TableId, request));
    }

    [Test]
    public void MarkTableBillAsPaid_UnauthorizedWaiter()
    {
        var session = ActiveSession();
        session.WaiterId = 999;
        var request = new MarkBillPaidDto { PaymentMethod = PaymentMethod.Cash };

        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId)).ReturnsAsync(session);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _waiterService.MarkTableBillAsPaid(WaiterId, TableId, request));
    }

   

    [Test]
    public async Task MarkOrderItemAsServed_Success()
    {
        var session   = ActiveSession();
        var orderItem = ReadyItem();
        var response  = new OrderItemResponseDto { OrderItemId = OrderItemId };

        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId)).ReturnsAsync(session);
        _orderItemRepoMock.Setup(r => r.GetOrderItemWithOrderAsync(OrderItemId)).ReturnsAsync(orderItem);
        _mapperMock.Setup(m => m.Map<OrderItemResponseDto>(orderItem)).Returns(response);

        var result = await _waiterService.MarkOrderItemAsServed(WaiterId, TableId, OrderItemId);

        Assert.Multiple(() =>
        {
            Assert.That(orderItem.Status, Is.EqualTo(OrderItemStatus.Served));
            Assert.That(result.OrderItemId, Is.EqualTo(OrderItemId));
        });
        _orderItemRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public void MarkOrderItemAsServed_SessionNotFound()
    {
        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId))
                        .ReturnsAsync((DiningSession?)null);

        Assert.ThrowsAsync<SessionNotFoundException>(
            () => _waiterService.MarkOrderItemAsServed(WaiterId, TableId, OrderItemId));
    }

    [Test]
    public void MarkOrderItemAsServed_UnauthorizedWaiter()
    {
        var session = ActiveSession();
        session.WaiterId = 999;

        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId)).ReturnsAsync(session);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _waiterService.MarkOrderItemAsServed(WaiterId, TableId, OrderItemId));
    }

    [Test]
    public void MarkOrderItemAsServed_OrderItemNotFound()
    {
        var session = ActiveSession();

        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId)).ReturnsAsync(session);
        _orderItemRepoMock.Setup(r => r.GetOrderItemWithOrderAsync(OrderItemId))
                          .ReturnsAsync((OrderItem?)null);

        Assert.ThrowsAsync<OrderItemNotFoundException>(
            () => _waiterService.MarkOrderItemAsServed(WaiterId, TableId, OrderItemId));
    }

    [Test]
    public void MarkOrderItemAsServed_OrderItemBelongsToDifferentSession()
    {
        var session   = ActiveSession();
        var orderItem = ReadyItem();
        orderItem.Order!.DiningSessionId = 9999; // belongs to another session

        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId)).ReturnsAsync(session);
        _orderItemRepoMock.Setup(r => r.GetOrderItemWithOrderAsync(OrderItemId)).ReturnsAsync(orderItem);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _waiterService.MarkOrderItemAsServed(WaiterId, TableId, OrderItemId));
    }

    [Test]
    public void MarkOrderItemAsServed_ItemAlreadyServed()
    {
        var session   = ActiveSession();
        var orderItem = ReadyItem();
        orderItem.Status = OrderItemStatus.Served;

        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId)).ReturnsAsync(session);
        _orderItemRepoMock.Setup(r => r.GetOrderItemWithOrderAsync(OrderItemId)).ReturnsAsync(orderItem);

        var ex = Assert.ThrowsAsync<ValidationException>(
            () => _waiterService.MarkOrderItemAsServed(WaiterId, TableId, OrderItemId));

        Assert.That(ex!.Message, Is.EqualTo("Item is already Served"));
    }

    [Test]
    public void MarkOrderItemAsServed_StatusIsNotReady()
    {
        var session   = ActiveSession();
        var orderItem = ReadyItem();
        orderItem.Status = OrderItemStatus.Placed;

        _sessionRepoMock.Setup(r => r.GetActiveSessionByTableId(TableId)).ReturnsAsync(session);
        _orderItemRepoMock.Setup(r => r.GetOrderItemWithOrderAsync(OrderItemId)).ReturnsAsync(orderItem);

        var ex = Assert.ThrowsAsync<ValidationException>(
            () => _waiterService.MarkOrderItemAsServed(WaiterId, TableId, OrderItemId));

        Assert.That(ex!.Message, Is.EqualTo("Only ready items can be marked as served."));
    }
}
