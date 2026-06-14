using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RestaurantAPI.Contexts;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;
using RestaurantAPI.Services;

namespace RestaurantAPI.Tests;

public class OrderServiceTests
{
    private Mock<IOrderRepository> _orderRepoMock;
    private Mock<IOrderItemRepository> _orderItemRepoMock;
    private Mock<IBillRepository> _billRepoMock;
    private Mock<ICartItemRepository> _cartItemRepoMock;
    private Mock<IDiningSessionRepository> _sessionRepoMock;
    private Mock<ICartRepository> _cartRepoMock;
    private Mock<IHubContext<NotificationHub>> _hubMock;
    private Mock<IHubClients> _clientsMock;
    private Mock<IClientProxy> _proxyMock;
    private Mock<IRestaurentTableRepository> _tableRepoMock;
    private Mock<IAuditService> _auditMock;
    private Mock<IUserRepository> _userRepoMock;
    private Mock<IBillService> _billServiceMock;
    private RestaurantContext _context;
    private OrderService _orderService;

    [SetUp]
    public void SetUp()
    {
        _orderRepoMock = new Mock<IOrderRepository>();
        _orderItemRepoMock = new Mock<IOrderItemRepository>();
        _billRepoMock = new Mock<IBillRepository>();
        _cartItemRepoMock = new Mock<ICartItemRepository>();
        _sessionRepoMock = new Mock<IDiningSessionRepository>();
        _cartRepoMock = new Mock<ICartRepository>();
        _tableRepoMock = new Mock<IRestaurentTableRepository>();
        _auditMock = new Mock<IAuditService>();
        _userRepoMock = new Mock<IUserRepository>();
        _billServiceMock = new Mock<IBillService>();

        _proxyMock = new Mock<IClientProxy>();
        _proxyMock
            .Setup(p => p.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _clientsMock = new Mock<IHubClients>();
        _clientsMock.Setup(c => c.User(It.IsAny<string>())).Returns(_proxyMock.Object);
        _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_proxyMock.Object);

        _hubMock = new Mock<IHubContext<NotificationHub>>();
        _hubMock.Setup(h => h.Clients).Returns(_clientsMock.Object);

        var options = new DbContextOptionsBuilder<RestaurantContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _context = new RestaurantContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _orderRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);
        _orderItemRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);
        _billRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);
        _cartItemRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);
        _billRepoMock
            .Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Bill>()))
            .ReturnsAsync((int _, Bill bill) => bill);
        _auditMock
            .Setup(a => a.LogAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuditAction>(),
                It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        _billServiceMock
            .Setup(s => s.RecalculateBill(It.IsAny<int>()))
            .Returns(Task.CompletedTask);
        _userRepoMock.Setup(r => r.GetKitchenStaffId()).ReturnsAsync(2);

        _orderService = new OrderService(
            _orderRepoMock.Object,
            _orderItemRepoMock.Object,
            _billRepoMock.Object,
            _cartItemRepoMock.Object,
            _sessionRepoMock.Object,
            _context,
            NullLogger<OrderService>.Instance,
            _cartRepoMock.Object,
            _hubMock.Object,
            _tableRepoMock.Object,
            _auditMock.Object,
            _userRepoMock.Object,
            _billServiceMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    private static DiningSession Session(int id = 10) => new()
    {
        Id = id,
        TableId = 1,
        WaiterId = 3,
        Status = DiningSessionStatus.Active,
        Table = new RestaurantTable { Id = 1, TableNumber = "T1" }
    };

    private static Bill PendingBill(int sessionId = 10) => new()
    {
        Id = 1,
        DiningSessionId = sessionId,
        BillNumber = "BILL-1",
        FoodTotal = 100m,
        CgstAmount = 5m,
        SgstAmount = 5m,
        ServiceChargeAmount = 5m,
        GrandTotal = 115m,
        PaymentStatus = PaymentStatus.Pending,
        TaxConfiguration = new TaxConfiguration
        {
            Id = 1,
            CgstPercentage = 5m,
            SgstPercentage = 5m,
            ServiceChargePercentage = 5m
        }
    };

    private static List<CartItem> AvailableCartItems() =>
    [
        new()
        {
            Id = 1,
            CartId = 20,
            MenuItemId = 101,
            Quantity = 2,
            MenuItem = new MenuItem
            {
                Id = 101, Name = "Burger", Price = 50m, IsAvailable = true
            }
        },
        new()
        {
            Id = 2,
            CartId = 20,
            MenuItemId = 102,
            Quantity = 1,
            MenuItem = new MenuItem
            {
                Id = 102, Name = "Pizza", Price = 100m, IsAvailable = true
            }
        }
    ];

    private static Order PlacedOrder(int itemCount = 2) => new()
    {
        Id = 50,
        OrderNumber = "20260614001",
        DiningSessionId = 10,
        DiningSession = Session(),
        TotalAmount = 200m,
        PlacedAt = DateTime.Now,
        OrderItems = Enumerable.Range(1, itemCount)
            .Select(index => new OrderItem
            {
                Id = index,
                MenuItemId = 100 + index,
                ItemName = $"Item {index}",
                ItemPrice = 50m,
                Quantity = 1,
                Status = OrderItemStatus.Placed
            })
            .ToList()
    };

    private void SetUpPlaceOrder(
        ICollection<CartItem>? cartItems = null,
        Bill? bill = null)
    {
        var session = Session();
        _sessionRepoMock.Setup(r => r.Get(10)).ReturnsAsync(session);
        _cartRepoMock
            .Setup(r => r.GetByDiningSessionId(10))
            .ReturnsAsync(new Cart { Id = 20, DiningSessionId = 10 });
        _cartItemRepoMock
            .Setup(r => r.GetByCartId(20))
            .ReturnsAsync(cartItems ?? AvailableCartItems());
        _billRepoMock
            .Setup(r => r.GetBySessionId(10))
            .ReturnsAsync(bill ?? PendingBill());
        _orderRepoMock
            .Setup(r => r.Create(It.IsAny<Order>()))
            .ReturnsAsync((Order order) =>
            {
                order.Id = 50;
                return order;
            });
        _orderItemRepoMock
            .Setup(r => r.Create(It.IsAny<OrderItem>()))
            .ReturnsAsync((OrderItem item) => item);
        _cartItemRepoMock
            .Setup(r => r.Delete(It.IsAny<int>()))
            .ReturnsAsync((int id) => new CartItem { Id = id });
        _orderRepoMock
            .Setup(r => r.GetLatestOrderNumberToday())
            .ReturnsAsync($"{DateTime.Today:yyyyMMdd}007");
        _tableRepoMock
            .Setup(r => r.Get(1))
            .ReturnsAsync(new RestaurantTable { Id = 1, TableNumber = "T1" });
    }

    [Test]
    public async Task PlaceOrder()
    {
        var bill = PendingBill();
        var createdItems = new List<OrderItem>();
        SetUpPlaceOrder(bill: bill);
        _orderItemRepoMock
            .Setup(r => r.Create(It.IsAny<OrderItem>()))
            .Callback<OrderItem>(createdItems.Add)
            .ReturnsAsync((OrderItem item) => item);

        await _orderService.PlaceOrder(10, new PlaceOrderRequestDto
        {
            SpecialInstructions = "No onions"
        });

        _orderRepoMock.Verify(r => r.Create(It.Is<Order>(o =>
            o.OrderNumber == $"{DateTime.Today:yyyyMMdd}008" &&
            o.TotalAmount == 200m &&
            o.SpecialInstructions == "No onions")), Times.Once);
        Assert.Multiple(() =>
        {
            Assert.That(createdItems, Has.Count.EqualTo(2));
            Assert.That(createdItems.Select(i => i.ItemName),
                Is.EquivalentTo(new[] { "Burger", "Pizza" }));
            Assert.That(bill.FoodTotal, Is.EqualTo(300m));
            Assert.That(bill.CgstAmount, Is.EqualTo(15m));
            Assert.That(bill.SgstAmount, Is.EqualTo(15m));
            Assert.That(bill.ServiceChargeAmount, Is.EqualTo(15m));
            Assert.That(bill.GrandTotal, Is.EqualTo(345m));
        });
        _cartItemRepoMock.Verify(r => r.Delete(It.IsAny<int>()), Times.Exactly(2));
        _proxyMock.Verify(p => p.SendCoreAsync(
            "ReceiveOrderPlaced",
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Test]
    public void PlaceOrder_SessionNotFound()
    {
        _sessionRepoMock.Setup(r => r.Get(10)).ReturnsAsync((DiningSession?)null);

        Assert.ThrowsAsync<SessionNotFoundException>(
            () => _orderService.PlaceOrder(10, new PlaceOrderRequestDto()));
    }

    [Test]
    public void PlaceOrder_EmptyCart()
    {
        SetUpPlaceOrder(Array.Empty<CartItem>());

        var ex = Assert.ThrowsAsync<CartException>(
            () => _orderService.PlaceOrder(10, new PlaceOrderRequestDto()));

        Assert.That(ex!.Message, Is.EqualTo("Cart is empty"));
    }

    [Test]
    public void PlaceOrder_MenuItemUnavailable()
    {
        var items = AvailableCartItems();
        items[0].MenuItem!.IsAvailable = false;
        SetUpPlaceOrder(items);

        Assert.ThrowsAsync<MenuItemUnavailableException>(
            () => _orderService.PlaceOrder(10, new PlaceOrderRequestDto()));
    }

    [Test]
    public void PlaceOrder_PaidBill()
    {
        var bill = PendingBill();
        bill.PaymentStatus = PaymentStatus.Paid;
        SetUpPlaceOrder(bill: bill);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _orderService.PlaceOrder(10, new PlaceOrderRequestDto()));
    }

    [Test]
    public async Task GetOrders_Sucess()
    {
        var order = PlacedOrder();
        order.OrderItems =
        [
            new OrderItem { Id = 1, ItemName = "Ready", Status = OrderItemStatus.Ready },
            new OrderItem { Id = 2, ItemName = "Placed", Status = OrderItemStatus.Placed }
        ];
        _sessionRepoMock.Setup(r => r.Get(10)).ReturnsAsync(Session());
        _orderRepoMock.Setup(r => r.GetBySessionId(10)).ReturnsAsync([order]);

        var result = await _orderService.GetOrders(10);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result.Single().OrderNumber, Is.EqualTo(order.OrderNumber));
            Assert.That(result.Single().Items.Select(i => i.Status),
                Is.EqualTo(new[] { OrderItemStatus.Placed, OrderItemStatus.Ready }));
        });
    }

    [Test]
    public void GetOrders_SessionNotFound()
    {
        _sessionRepoMock.Setup(r => r.Get(10)).ReturnsAsync((DiningSession?)null);

        Assert.ThrowsAsync<SessionNotFoundException>(() => _orderService.GetOrders(10));
    }

    [Test]
    public async Task GetTodayOrderCount()
    {
        _orderRepoMock.Setup(r => r.GetTodayOrderCount()).ReturnsAsync(12);

        var result = await _orderService.GetTodayOrderCount();

        Assert.That(result, Is.EqualTo(12));
    }

    [Test]
    public async Task GetKitchenOrders()
    {
        var order = PlacedOrder(1);
        _orderRepoMock
            .Setup(r => r.GetKitchenOrders(OrderItemStatus.Placed))
            .ReturnsAsync([order]);
        _orderRepoMock.Setup(r => r.GetKitchenQueueCount()).ReturnsAsync(4);
        _orderRepoMock.Setup(r => r.GetKitchenPreparingCount()).ReturnsAsync(2);
        _orderRepoMock.Setup(r => r.GetKitchenReadyCount()).ReturnsAsync(1);

        var result = await _orderService.GetKitchenOrders(OrderItemStatus.Placed);

        Assert.Multiple(() =>
        {
            Assert.That(result.QueueCount, Is.EqualTo(4));
            Assert.That(result.PreparingCount, Is.EqualTo(2));
            Assert.That(result.ReadyCount, Is.EqualTo(1));
            Assert.That(result.Orders.Single().TableNumber, Is.EqualTo("T1"));
            Assert.That(result.Orders.Single().Items, Has.Count.EqualTo(1));
        });
    }

    [TestCase(OrderItemStatus.Served)]
    [TestCase(OrderItemStatus.Cancelled)]
    public void GetKitchenOrders_InvalidStatus(OrderItemStatus status)
    {
        Assert.ThrowsAsync<InvalidOperationException>(
            () => _orderService.GetKitchenOrders(status));
    }

    [Test]
    public async Task CancelOrder_CancelsItemsRecalculatesBillAndAudits()
    {
        var order = PlacedOrder();
        _orderRepoMock.Setup(r => r.GetOrderWithItems(50)).ReturnsAsync(order);
        _billRepoMock
            .Setup(r => r.GetBySessionId(order.DiningSessionId))
            .ReturnsAsync(PendingBill());

        await _orderService.CancelOrder(50);

        Assert.Multiple(() =>
        {
            Assert.That(order.CancelledAt, Is.Not.Null);
            Assert.That(order.OrderItems!.All(i => i.Status == OrderItemStatus.Cancelled),
                Is.True);
        });
        _billServiceMock.Verify(s => s.RecalculateBill(10), Times.Once);
        _auditMock.Verify(a => a.LogAsync(
            nameof(Order), "50", AuditAction.Cancelled,
            null, null, "Order 50 cancelled"), Times.Once);
        _proxyMock.Verify(p => p.SendCoreAsync(
            "OrderCancelled",
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Test]
    public void CancelOrder_OrderNotFound()
    {
        _orderRepoMock.Setup(r => r.GetOrderWithItems(99)).ReturnsAsync((Order?)null);

        Assert.ThrowsAsync<OrderNotFoundException>(() => _orderService.CancelOrder(99));
    }

    [Test]
    public void CancelOrder_ItemStatusWrong()
    {
        var order = PlacedOrder();
        order.OrderItems!.First().Status = OrderItemStatus.Preparing;
        _orderRepoMock.Setup(r => r.GetOrderWithItems(50)).ReturnsAsync(order);

        var ex = Assert.ThrowsAsync<Exception>(() => _orderService.CancelOrder(50));

        Assert.That(ex!.Message, Is.EqualTo("Only orders in placed status can be cancelled"));
    }

    [Test]
    public void CancelOrder_PaidBill()
    {
        var order = PlacedOrder();
        var bill = PendingBill();
        bill.PaymentStatus = PaymentStatus.Paid;
        _orderRepoMock.Setup(r => r.GetOrderWithItems(50)).ReturnsAsync(order);
        _billRepoMock.Setup(r => r.GetBySessionId(10)).ReturnsAsync(bill);

        var ex = Assert.ThrowsAsync<Exception>(() => _orderService.CancelOrder(50));

        Assert.That(ex!.Message, Is.EqualTo("Cannot cancel order after payment"));
    }

    [Test]
    public async Task CancelOrderItem_CreatesReplacementWithoutRemovedItem()
    {
        var order = PlacedOrder();
        Order? replacement = null;
        _orderRepoMock.Setup(r => r.GetOrderWithItems(50)).ReturnsAsync(order);
        _billRepoMock.Setup(r => r.GetBySessionId(10)).ReturnsAsync(PendingBill());
        _orderRepoMock
            .Setup(r => r.Create(It.IsAny<Order>()))
            .Callback<Order>(created => replacement = created)
            .ReturnsAsync((Order created) =>
            {
                created.Id = 60;
                return created;
            });

        await _orderService.CancelOrderItem(50, 1);

        Assert.Multiple(() =>
        {
            Assert.That(order.CancelledAt, Is.Not.Null);
            Assert.That(order.OrderItems!.All(i => i.Status == OrderItemStatus.Cancelled),
                Is.True);
            Assert.That(replacement, Is.Not.Null);
            Assert.That(replacement!.OrderItems, Has.Count.EqualTo(1));
            Assert.That(replacement.OrderItems!.Single().MenuItemId, Is.EqualTo(102));
            Assert.That(replacement.TotalAmount, Is.EqualTo(50m));
        });
        _billServiceMock.Verify(s => s.RecalculateBill(10), Times.Once);
        _proxyMock.Verify(p => p.SendCoreAsync(
            "OrderModified",
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Test]
    public void CancelOrderItem_NonPlacedItem()
    {
        var order = PlacedOrder();
        order.OrderItems!.Last().Status = OrderItemStatus.Preparing;
        _orderRepoMock.Setup(r => r.GetOrderWithItems(50)).ReturnsAsync(order);
        _billRepoMock.Setup(r => r.GetBySessionId(10)).ReturnsAsync(PendingBill());

        var ex = Assert.ThrowsAsync<Exception>(
            () => _orderService.CancelOrderItem(50, 1));

        Assert.That(ex!.Message, Is.EqualTo("Only orders in placed status can be modified"));
    }

    [Test]
    public async Task UpdateOrderItemQuantity_CreatesReplacementWithLowerQuantity()
    {
        var order = PlacedOrder();
        order.OrderItems!.First().Quantity = 3;
        Order? replacement = null;
        _orderRepoMock.Setup(r => r.GetOrderWithItems(50)).ReturnsAsync(order);
        _billRepoMock.Setup(r => r.GetBySessionId(10)).ReturnsAsync(PendingBill());
        _orderRepoMock
            .Setup(r => r.Create(It.IsAny<Order>()))
            .Callback<Order>(created => replacement = created)
            .ReturnsAsync((Order created) =>
            {
                created.Id = 61;
                return created;
            });

        await _orderService.UpdateOrderItemQuantity(50, 1, 2);

        Assert.Multiple(() =>
        {
            Assert.That(order.CancelledAt, Is.Not.Null);
            Assert.That(replacement, Is.Not.Null);
            Assert.That(replacement!.OrderItems!.Single(i => i.MenuItemId == 101).Quantity,
                Is.EqualTo(2));
            Assert.That(replacement.TotalAmount, Is.EqualTo(150m));
        });
        _billServiceMock.Verify(s => s.RecalculateBill(10), Times.Once);
        _proxyMock.Verify(p => p.SendCoreAsync(
            "OrderModified",
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [TestCase(0, "Quantity must be greater than zero")]
    [TestCase(-1, "Quantity must be greater than zero")]
    [TestCase(3, "Updated quantity must be less than existing quantity")]
    [TestCase(4, "Updated quantity must be less than existing quantity")]
    public void UpdateOrderItemQuantity_InvalidQuantity(int quantity, string expectedMessage)
    {
        var order = PlacedOrder();
        order.OrderItems!.First().Quantity = 3;
        _orderRepoMock.Setup(r => r.GetOrderWithItems(50)).ReturnsAsync(order);
        _billRepoMock.Setup(r => r.GetBySessionId(10)).ReturnsAsync(PendingBill());

        var ex = Assert.ThrowsAsync<Exception>(
            () => _orderService.UpdateOrderItemQuantity(50, 1, quantity));

        Assert.That(ex!.Message, Is.EqualTo(expectedMessage));
    }

    [Test]
    public async Task GetAllOrders()
    {
        var today = DateTime.Today;
        var orders = new[]
        {
            RegistryOrder(1, "ORDER-OLD", today.AddDays(-1), DiningSessionStatus.Completed),
            RegistryOrder(2, "ORDER-TODAY", today.AddHours(10), DiningSessionStatus.Active),
            RegistryOrder(3, "SPECIAL-TODAY", today.AddHours(12), DiningSessionStatus.Active,
                cancelled: true)
        };
        _orderRepoMock
            .Setup(r => r.GetOrdersQuery())
            .Returns(new TestAsyncEnumerable<Order>(orders));

        var result = await _orderService.GetAllOrders(
            " SPECIAL ", DateOnly.FromDateTime(today), 1, 10);

        Assert.Multiple(() =>
        {
            Assert.That(result.Items, Has.Count.EqualTo(1));
            Assert.That(result.Items.Single().OrderNumber, Is.EqualTo("SPECIAL-TODAY"));
            Assert.That(result.Items.Single().Status, Is.EqualTo("Cancelled"));
            Assert.That(result.Items.Single().ItemCount, Is.EqualTo(1));
            Assert.That(result.PageNumber, Is.EqualTo(1));
            Assert.That(result.PageSize, Is.EqualTo(10));
        });
    }

    [TestCase(0, 0, 1, 40)]
    [TestCase(1, 500, 1, 100)]
    public async Task GetAllOrders_Pagination(
        int pageNumber,
        int pageSize,
        int expectedPageNumber,
        int expectedPageSize)
    {
        _orderRepoMock
            .Setup(r => r.GetOrdersQuery())
            .Returns(new TestAsyncEnumerable<Order>(Array.Empty<Order>()));

        var result = await _orderService.GetAllOrders(
            string.Empty, null, pageNumber, pageSize);

        Assert.Multiple(() =>
        {
            Assert.That(result.PageNumber, Is.EqualTo(expectedPageNumber));
            Assert.That(result.PageSize, Is.EqualTo(expectedPageSize));
        });
    }

    [Test]
    public void GetAllOrders_FutureDateThrows()
    {
        _orderRepoMock
            .Setup(r => r.GetOrdersQuery())
            .Returns(new TestAsyncEnumerable<Order>(Array.Empty<Order>()));
        var futureDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        var ex = Assert.ThrowsAsync<Exception>(
            () => _orderService.GetAllOrders(string.Empty, futureDate, 1, 40));

        Assert.That(ex!.Message, Is.EqualTo("Future dates not allowed"));
    }

    [Test]
    public async Task GetOrderDetails()
    {
        var order = PlacedOrder();
        order.DiningSession!.Status = DiningSessionStatus.Completed;
        order.DiningSession.Bill = new Bill
        {
            BillNumber = "BILL-10",
            GrandTotal = 250m,
            PaymentMethod = PaymentMethod.Card
        };
        _orderRepoMock.Setup(r => r.GetOrderDetails(50)).ReturnsAsync(order);

        var result = await _orderService.GetOrderDetails(50);

        Assert.Multiple(() =>
        {
            Assert.That(result.OrderNumber, Is.EqualTo(order.OrderNumber));
            Assert.That(result.TableNumber, Is.EqualTo("T1"));
            Assert.That(result.Status, Is.EqualTo("Completed"));
            Assert.That(result.BillNumber, Is.EqualTo("BILL-10"));
            Assert.That(result.BillTotal, Is.EqualTo(250m));
            Assert.That(result.PaymentMethod, Is.EqualTo(PaymentMethod.Card));
            Assert.That(result.Items, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void GetOrderDetails_OrderNotFound()
    {
        _orderRepoMock.Setup(r => r.GetOrderDetails(99)).ReturnsAsync((Order?)null);

        Assert.ThrowsAsync<OrderNotFoundException>(() => _orderService.GetOrderDetails(99));
    }

    private static Order RegistryOrder(
        int id,
        string orderNumber,
        DateTime placedAt,
        DiningSessionStatus sessionStatus,
        bool cancelled = false) => new()
    {
        Id = id,
        OrderNumber = orderNumber,
        PlacedAt = placedAt,
        CancelledAt = cancelled ? placedAt.AddMinutes(5) : null,
        DiningSession = new DiningSession
        {
            Status = sessionStatus,
            Table = new RestaurantTable { TableNumber = $"T{id}" }
        },
        OrderItems =
        [
            new OrderItem
            {
                Id = id,
                ItemName = "Item",
                Quantity = 1,
                Status = OrderItemStatus.Placed
            }
        ]
    };
}
