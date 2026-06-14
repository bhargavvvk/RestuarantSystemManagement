using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RestaurantAPI;
using RestaurantAPI.Contexts;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;
using RestaurantAPI.Services;

namespace RestaurantAPI.Tests;

public class AdminServiceTests
{
    private Mock<IRestaurentTableRepository>  _tableRepoMock;
    private Mock<IUserRepository>             _userRepoMock;
    private Mock<IDiningSessionRepository>    _sessionRepoMock;
    private Mock<IAuditService>               _auditMock;
    private Mock<IHubContext<NotificationHub>> _hubMock;
    private Mock<IClientProxy>                 _proxyMock;
    private Mock<IIOrderService>               _orderServiceMock;
    private Mock<IBillService>                 _billServiceMock;
    private Mock<IOrderRepository>             _orderRepoMock;
    private RestaurantContext                 _context;
    private AdminService  _adminService;

    [SetUp]
    public void SetUp()
    {
        _tableRepoMock   = new Mock<IRestaurentTableRepository>();
        _userRepoMock    = new Mock<IUserRepository>();
        _sessionRepoMock = new Mock<IDiningSessionRepository>();
        _auditMock       = new Mock<IAuditService>();
        _orderServiceMock = new Mock<IIOrderService>();
        _billServiceMock = new Mock<IBillService>();
        _orderRepoMock = new Mock<IOrderRepository>();

        var clientsMock = new Mock<IHubClients>();
        _proxyMock = new Mock<IClientProxy>();
        _proxyMock
            .Setup(p => p.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        clientsMock
            .Setup(c => c.User(It.IsAny<string>()))
            .Returns(_proxyMock.Object);
        _hubMock = new Mock<IHubContext<NotificationHub>>();
        _hubMock.Setup(h => h.Clients).Returns(clientsMock.Object);

       
        var options = new DbContextOptionsBuilder<RestaurantContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _context = new RestaurantContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _auditMock
            .Setup(a => a.LogAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuditAction>(),
                It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        _tableRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);
        _userRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

         _adminService = new AdminService(
            _tableRepoMock.Object,
            _orderServiceMock.Object,
            _billServiceMock.Object,
            _orderRepoMock.Object,
            _userRepoMock.Object,
            _sessionRepoMock.Object,
            _auditMock.Object,
            _context,
            _hubMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }


    private static RestaurantTable ActiveTable(int id = 1) => new()
    {
        Id             = id,
        TableNumber    = "T1",
        QrIdentifier   = "qr1",
        Capacity       = 4,
        IsDeleted      = false,
        AssignedWaiterId = null
    };

    private static User ActiveWaiter(int id = 10) => new()
    {
        Id       = id,
        Name     = "John",
        Role     = UserRole.Waiter,
        IsActive = true,
        IsDeleted = false
    };

    private static RestaurantTable TableWithActiveSession(
        int tableId = 1,
        int sessionId = 20) => new()
    {
        Id = tableId,
        TableNumber = "T1",
        QrIdentifier = "qr1",
        Capacity = 4,
        DiningSessions =
        [
            new DiningSession
            {
                Id = sessionId,
                TableId = tableId,
                Status = DiningSessionStatus.Active
            }
        ]
    };

    [Test]
    public async Task GetTableOrders()
    {
        var table = TableWithActiveSession();
        var expected = new List<OrderResponseDto>
        {
            new() { OrderId = 1, OrderNumber = "ORDER-1" }
        };
        _tableRepoMock.Setup(r => r.GetTableDetails(1)).ReturnsAsync(table);
        _orderServiceMock.Setup(s => s.GetOrders(20)).ReturnsAsync(expected);

        var result = await _adminService.GetTableOrders(1);

        Assert.That(result, Is.SameAs(expected));
        _orderServiceMock.Verify(s => s.GetOrders(20), Times.Once);
    }

    [Test]
    public void GetTableOrders_TableNotFound()
    {
        _tableRepoMock
            .Setup(r => r.GetTableDetails(99))
            .ReturnsAsync((RestaurantTable?)null);

        Assert.ThrowsAsync<TableNotFoundException>(
            () => _adminService.GetTableOrders(99));
    }

    [Test]
    public async Task GetTableBill_ReturnsBillForActiveSession()
    {
        var expected = new BillResponseDto { BillNumber = "BILL-1" };
        _tableRepoMock
            .Setup(r => r.GetTableDetails(1))
            .ReturnsAsync(TableWithActiveSession());
        _billServiceMock.Setup(s => s.GetBill(20)).ReturnsAsync(expected);

        var result = await _adminService.GetTableBill(1);

        Assert.That(result, Is.SameAs(expected));
        _billServiceMock.Verify(s => s.GetBill(20), Times.Once);
    }

    [Test]
    public void GetTableBill_ActiveSessionNotFound()
    {
        var table = ActiveTable();
        table.DiningSessions =
        [
            new DiningSession { Id = 10, Status = DiningSessionStatus.Completed }
        ];
        _tableRepoMock.Setup(r => r.GetTableDetails(1)).ReturnsAsync(table);

        Assert.ThrowsAsync<SessionNotFoundException>(
            () => _adminService.GetTableBill(1));
    }

    [Test]
    public async Task UpdateServiceCharge_UsesActiveSession()
    {
        var expected = new BillResponseDto { ServiceChargeAmount = 25m };
        _tableRepoMock
            .Setup(r => r.GetTableDetails(1))
            .ReturnsAsync(TableWithActiveSession());
        _billServiceMock
            .Setup(s => s.UpdateServiceCharge(20, false))
            .ReturnsAsync(expected);

        var result = await _adminService.UpdateServiceCharge(1, false);

        Assert.That(result, Is.SameAs(expected));
        _billServiceMock.Verify(
            s => s.UpdateServiceCharge(20, false), Times.Once);
    }

    [Test]
    public async Task CancelOrder()
    {
        _tableRepoMock
            .Setup(r => r.GetTableDetails(1))
            .ReturnsAsync(TableWithActiveSession());
        _orderRepoMock
            .Setup(r => r.Get(50))
            .ReturnsAsync(new Order { Id = 50, DiningSessionId = 20 });
        _orderServiceMock
            .Setup(s => s.CancelOrder(50))
            .Returns(Task.CompletedTask);

        await _adminService.CancelOrder(1, 50);

        _orderServiceMock.Verify(s => s.CancelOrder(50), Times.Once);
    }

    [Test]
    public void CancelOrder_OrderNotFound()
    {
        _tableRepoMock
            .Setup(r => r.GetTableDetails(1))
            .ReturnsAsync(TableWithActiveSession());
        _orderRepoMock.Setup(r => r.Get(99)).ReturnsAsync((Order?)null);

        Assert.ThrowsAsync<OrderNotFoundException>(
            () => _adminService.CancelOrder(1, 99));
    }

    [Test]
    public void CancelOrder_OrderBelongsToAnotherTable()
    {
        _tableRepoMock
            .Setup(r => r.GetTableDetails(1))
            .ReturnsAsync(TableWithActiveSession());
        _orderRepoMock
            .Setup(r => r.Get(50))
            .ReturnsAsync(new Order { Id = 50, DiningSessionId = 999 });

        var ex = Assert.ThrowsAsync<Exception>(
            () => _adminService.CancelOrder(1, 50));

        Assert.That(ex!.Message,
            Is.EqualTo("Order does not belong to the specified table"));
        _orderServiceMock.Verify(s => s.CancelOrder(It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task CancelOrderItem()
    {
        _tableRepoMock
            .Setup(r => r.GetTableDetails(1))
            .ReturnsAsync(TableWithActiveSession());
        _orderRepoMock
            .Setup(r => r.Get(50))
            .ReturnsAsync(new Order { Id = 50, DiningSessionId = 20 });
        _orderServiceMock
            .Setup(s => s.CancelOrderItem(50, 5))
            .Returns(Task.CompletedTask);

        await _adminService.CancelOrderItem(1, 50, 5);

        _orderServiceMock.Verify(s => s.CancelOrderItem(50, 5), Times.Once);
    }

    [Test]
    public void CancelOrderItem_fail()
    {
        _tableRepoMock
            .Setup(r => r.GetTableDetails(1))
            .ReturnsAsync(TableWithActiveSession());
        _orderRepoMock
            .Setup(r => r.Get(50))
            .ReturnsAsync(new Order { Id = 50, DiningSessionId = 999 });

        var ex = Assert.ThrowsAsync<Exception>(
            () => _adminService.CancelOrderItem(1, 50, 5));

        Assert.That(ex!.Message,
            Is.EqualTo("Order does not belong to the specified table"));
    }

    [Test]
    public async Task UpdateOrderItemQuantity()
    {
        _tableRepoMock
            .Setup(r => r.GetTableDetails(1))
            .ReturnsAsync(TableWithActiveSession());
        _orderRepoMock
            .Setup(r => r.Get(50))
            .ReturnsAsync(new Order { Id = 50, DiningSessionId = 20 });
        _orderServiceMock
            .Setup(s => s.UpdateOrderItemQuantity(50, 5, 2))
            .Returns(Task.CompletedTask);

        await _adminService.UpdateOrderItemQuantity(1, 50, 5, 2);

        _orderServiceMock.Verify(
            s => s.UpdateOrderItemQuantity(50, 5, 2), Times.Once);
    }

    [Test]
    public void UpdateOrderItemQuantity_fail()
    {
        _tableRepoMock
            .Setup(r => r.GetTableDetails(1))
            .ReturnsAsync(TableWithActiveSession());
        _orderRepoMock
            .Setup(r => r.Get(50))
            .ReturnsAsync(new Order { Id = 50, DiningSessionId = 999 });

        var ex = Assert.ThrowsAsync<Exception>(
            () => _adminService.UpdateOrderItemQuantity(1, 50, 5, 2));

        Assert.That(ex!.Message,
            Is.EqualTo("Order does not belong to the specified table"));
    }

    [Test]
    public async Task GetWaiters()
    {
        var waiters = new[]
        {
            ActiveWaiter(10),
            new User
            {
                Id = 11, Name = "Alice", Role = UserRole.Waiter, IsActive = true
            },
            new User
            {
                Id = 12, Name = "Bob", Role = UserRole.Waiter, IsActive = false
            }
        };
        _userRepoMock
            .Setup(r => r.GetWaitersQuery())
            .Returns(new TestAsyncEnumerable<User>(waiters));
        _tableRepoMock.Setup(r => r.GetAll()).ReturnsAsync(
        [
            new RestaurantTable
            {
                TableNumber = "T1", AssignedWaiterId = 11, IsDeleted = false
            },
            new RestaurantTable
            {
                TableNumber = "T2", AssignedWaiterId = 11, IsDeleted = true
            },
            new RestaurantTable
            {
                TableNumber = "T3", AssignedWaiterId = 10, IsDeleted = false
            }
        ]);

        var result = await _adminService.GetWaiters(" ALI ", true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Summary.TotalWaiters, Is.EqualTo(3));
            Assert.That(result.Summary.ActiveWaiters, Is.EqualTo(2));
            Assert.That(result.Summary.InactiveWaiters, Is.EqualTo(1));
            Assert.That(result.Waiters, Has.Count.EqualTo(1));
            Assert.That(result.Waiters.Single().Name, Is.EqualTo("Alice"));
            Assert.That(result.Waiters.Single().AssignedTableCount, Is.EqualTo(1));
            Assert.That(result.Waiters.Single().AssignedTables,
                Is.EqualTo(new[] { "T1" }));
        });
    }


    [Test]
    public async Task AssignWaiter_Sucess()
    {
        var table  = ActiveTable();
        var waiter = ActiveWaiter();

        _tableRepoMock.Setup(r => r.Get(1)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.HasActiveSession(1)).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.GetActiveWaiter(10)).ReturnsAsync(waiter);

        var result = await  _adminService.AssignWaiter(1, 10);

        Assert.That(result.AssignedWaiterId, Is.EqualTo(10));
        _tableRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public void AssignWaiter_TableNotFound()
    {
        _tableRepoMock.Setup(r => r.Get(99)).ReturnsAsync((RestaurantTable?)null);

        Assert.ThrowsAsync<TableNotFoundException>(
            () =>  _adminService.AssignWaiter(99, 10));
    }

    [Test]
    public void AssignWaiter_WaiterNotFound()
    {
        var table = ActiveTable();

        _tableRepoMock.Setup(r => r.Get(1)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.HasActiveSession(1)).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.GetActiveWaiter(99)).ReturnsAsync((User?)null);

        Assert.ThrowsAsync<WaiterNotFoundException>(
            () =>  _adminService.AssignWaiter(1, 99));
    }
    [Test]
    public void AssignWaiter_ActiveSessionExists()
    {
        var table = ActiveTable();

        _tableRepoMock.Setup(r => r.Get(1)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.HasActiveSession(1)).ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<Exception>(
            () =>  _adminService.AssignWaiter(1, 10));

        Assert.That(ex!.Message, Is.EqualTo("Cannot change waiter assignment during active dining session"));
    }


    [Test]
    public async Task RemoveWaiter_Success()
    {
        var table = ActiveTable();
        table.AssignedWaiterId = 10;

        _tableRepoMock.Setup(r => r.Get(1)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.HasActiveSession(1)).ReturnsAsync(false);

        var result = await  _adminService.RemoveWaiter(1);

        Assert.That(result.AssignedWaiterId, Is.Null);
        Assert.That(result.Status, Is.EqualTo(TableStatus.Unavailable));
        _tableRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public void RemoveWaiter_TableNotFound()
    {
        _tableRepoMock
            .Setup(r => r.Get(99))
            .ReturnsAsync((RestaurantTable?)null);

        Assert.ThrowsAsync<TableNotFoundException>(
            () => _adminService.RemoveWaiter(99));
    }

    [Test]
    public void RemoveWaiter_ActiveSessionExists()
    {
        _tableRepoMock.Setup(r => r.Get(1)).ReturnsAsync(ActiveTable());
        _sessionRepoMock.Setup(r => r.HasActiveSession(1)).ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<Exception>(
            () => _adminService.RemoveWaiter(1));

        Assert.That(ex!.Message,
            Is.EqualTo("Cannot change waiter assignment during active dining session"));
    }


    [Test]
    public async Task UpdateWaiterStatus_sucess()
    {
        var waiter = ActiveWaiter();
        waiter.IsActive = true;

        _userRepoMock.Setup(r => r.Get(10)).ReturnsAsync(waiter);
        _tableRepoMock.Setup(r => r.HasAssignedTables(10)).ReturnsAsync(false);

        await  _adminService.UpdateWaiterStatus(10, false);

        Assert.That(waiter.IsActive, Is.False);
        _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task UpdateWaiterStatus_ActivateWaiter()
    {
        var waiter = ActiveWaiter();
        waiter.IsActive = false;
        _userRepoMock.Setup(r => r.Get(10)).ReturnsAsync(waiter);

        await _adminService.UpdateWaiterStatus(10, true);

        Assert.That(waiter.IsActive, Is.True);
        _tableRepoMock.Verify(
            r => r.HasAssignedTables(It.IsAny<int>()), Times.Never);
        _auditMock.Verify(a => a.LogAsync(
            nameof(User), "10", AuditAction.Updated,
            It.IsAny<object>(), It.IsAny<object>(), "Waiter activated"), Times.Once);
    }

    [Test]
    public void UpdateWaiterStatus_WaiterNotFound()
    {
        _userRepoMock.Setup(r => r.Get(99)).ReturnsAsync((User?)null);

        Assert.ThrowsAsync<WaiterNotFoundException>(
            () =>  _adminService.UpdateWaiterStatus(99, false));
    }

    [Test]
    public void UpdateWaiterStatus_UserIsNotWaiter()
    {
        var admin = ActiveWaiter();
        admin.Role = UserRole.Admin;
        _userRepoMock.Setup(r => r.Get(10)).ReturnsAsync(admin);

        Assert.ThrowsAsync<WaiterNotFoundException>(
            () => _adminService.UpdateWaiterStatus(10, false));
    }

    [Test]
    public void UpdateWaiterStatus__WaiterHasAssignedTables()
    {
        var waiter = ActiveWaiter();

        _userRepoMock.Setup(r => r.Get(10)).ReturnsAsync(waiter);
        _tableRepoMock.Setup(r => r.HasAssignedTables(10)).ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<Exception>(
            () =>  _adminService.UpdateWaiterStatus(10, false));

        Assert.That(ex!.Message, Is.EqualTo("Cannot deactivate waiter while tables are assigned"));
    }


    [Test]
    public async Task DeleteWaiter_Sucess()
    {
        var waiter = ActiveWaiter();

        _userRepoMock.Setup(r => r.Get(10)).ReturnsAsync(waiter);
        _tableRepoMock.Setup(r => r.HasAssignedTables(10)).ReturnsAsync(false);

        await  _adminService.DeleteWaiter(10);

        Assert.Multiple(() =>
        {
            Assert.That(waiter.IsDeleted, Is.True);
            Assert.That(waiter.IsActive,  Is.False);
        });
        _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));
    }

    [Test]
    public void DeleteWaiter_WaiterNotFound()
    {
        _userRepoMock.Setup(r => r.Get(99)).ReturnsAsync((User?)null);

        Assert.ThrowsAsync<WaiterNotFoundException>(
            () =>  _adminService.DeleteWaiter(99));
    }

    [Test]
    public void DeleteWaiter_HasAssignedTables()
    {
        _userRepoMock.Setup(r => r.Get(10)).ReturnsAsync(ActiveWaiter());
        _tableRepoMock.Setup(r => r.HasAssignedTables(10)).ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<Exception>(
            () => _adminService.DeleteWaiter(10));

        Assert.That(ex!.Message,
            Is.EqualTo("Cannot delete waiter while tables are assigned"));
        _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}
