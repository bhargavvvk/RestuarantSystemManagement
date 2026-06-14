using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RestaurantAPI;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.Services;

namespace RestaurantAPI.Tests;


public class KitchenServiceTests
{
    private Mock<IOrderRepository>          _orderRepoMock;
    private Mock<IHubContext<NotificationHub>> _hubMock;
    private KitchenService _kitchenService;

    [SetUp]
    public void SetUp()
    {
        _orderRepoMock = new Mock<IOrderRepository>();
        _hubMock = new Mock<IHubContext<NotificationHub>>();

        var clientsMock = new Mock<IHubClients>();
        var proxyMock   = new Mock<IClientProxy>();
        proxyMock
            .Setup(p => p.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        clientsMock
            .Setup(c => c.User(It.IsAny<string>()))
            .Returns(proxyMock.Object);
        _hubMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        _orderRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);
        _kitchenService = new KitchenService(
            _orderRepoMock.Object,
            NullLogger<KitchenService>.Instance,
            _hubMock.Object);
    }


    private static Order OrderWithAllPlaced(int orderId = 1) => new()
    {
        Id           = orderId,
        CancelledAt  = null,
        DiningSession = new DiningSession
        {
            WaiterId = 5,
            Table    = new RestaurantTable { TableNumber = "3" }
        },
        OrderItems = new List<OrderItem>
        {
            new() { Id = 10, Status = OrderItemStatus.Placed, ItemName = "Burger" },
            new() { Id = 11, Status = OrderItemStatus.Placed, ItemName = "Pizza"  }
        }
    };

    private static Order OrderWithPreparingItems(int orderId = 1) => new()
    {
        Id           = orderId,
        CancelledAt  = null,
        DiningSession = new DiningSession
        {
            WaiterId = 5,
            Table    = new RestaurantTable { TableNumber = "3" }
        },
        OrderItems = new List<OrderItem>
        {
            new() { Id = 10, Status = OrderItemStatus.Preparing, ItemName = "Burger" }
        }
    };


    [Test]
    public async Task StartPreparing_Sucess()
    {
        var order = OrderWithAllPlaced();
        _orderRepoMock.Setup(r => r.GetOrderWithItems(1)).ReturnsAsync(order);

        await _kitchenService.StartPreparing(1);

        Assert.That(order.OrderItems!.All(i => i.Status == OrderItemStatus.Preparing), Is.True);
        _orderRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public void StartPreparing_OrderNotFound()
    {
        _orderRepoMock.Setup(r => r.GetOrderWithItems(99)).ReturnsAsync((Order?)null);

        Assert.ThrowsAsync<OrderNotFoundException>(
            () => _kitchenService.StartPreparing(99));
    }

    [Test]
    public void StartPreparingItemsNotAllInPlacedStatus()
    {
        var order = OrderWithAllPlaced();
        order.OrderItems!.First().Status = OrderItemStatus.Preparing; 
        _orderRepoMock.Setup(r => r.GetOrderWithItems(1)).ReturnsAsync(order);

        var ex = Assert.ThrowsAsync<Exception>(
            () => _kitchenService.StartPreparing(1));

        Assert.That(ex!.Message, Is.EqualTo("All order items must be in Placed status."));
    }

   

    [Test]
    public async Task MarkOrderItemReady_sucess()
    {
        var order = OrderWithPreparingItems();
        _orderRepoMock.Setup(r => r.GetOrderWithItems(1)).ReturnsAsync(order);

        await _kitchenService.MarkOrderItemReady(1, 10);

        Assert.That(order.OrderItems!.First(i => i.Id == 10).Status, Is.EqualTo(OrderItemStatus.Ready));
        _orderRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }


    [Test]
    public void MarkOrderItemReady_OrderItemNotFound()
    {
        var order = OrderWithPreparingItems();
        _orderRepoMock.Setup(r => r.GetOrderWithItems(1)).ReturnsAsync(order);

        Assert.ThrowsAsync<OrderItemNotFoundException>(
            () => _kitchenService.MarkOrderItemReady(1, 999)); 
    }

    [Test]
    public void MarkOrderItemReady_failure()
    {
        var order = OrderWithPreparingItems();
        order.OrderItems!.First().Status = OrderItemStatus.Placed; // wrong status
        _orderRepoMock.Setup(r => r.GetOrderWithItems(1)).ReturnsAsync(order);

        var ex = Assert.ThrowsAsync<Exception>(
            () => _kitchenService.MarkOrderItemReady(1, 10));

        Assert.That(ex!.Message, Is.EqualTo("Only preparing items can be marked as ready"));
    }
}
