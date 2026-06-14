using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RestaurantAPI;
using RestaurantAPI.Contexts;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
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
    private RestaurantContext                 _context;
    private AdminService  _adminService;

    [SetUp]
    public void SetUp()
    {
        _tableRepoMock   = new Mock<IRestaurentTableRepository>();
        _userRepoMock    = new Mock<IUserRepository>();
        _sessionRepoMock = new Mock<IDiningSessionRepository>();
        _auditMock       = new Mock<IAuditService>();

       
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
            Mock.Of<IIOrderService>(),
            Mock.Of<IBillService>(),
            Mock.Of<IOrderRepository>(),
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
        _tableRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
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
    public void UpdateWaiterStatus_WaiterNotFound()
    {
        _userRepoMock.Setup(r => r.Get(99)).ReturnsAsync((User?)null);

        Assert.ThrowsAsync<WaiterNotFoundException>(
            () =>  _adminService.UpdateWaiterStatus(99, false));
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
}
