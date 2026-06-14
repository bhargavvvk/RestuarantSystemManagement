using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;
using RestaurantAPI.Services;

namespace RestaurantAPI.Tests;

[TestFixture]
public class TableServiceTests
{
    private Mock<IRestaurentTableRepository> _tableRepoMock;
    private Mock<IDiningSessionRepository>   _sessionRepoMock;
    private Mock<IAuditService>              _auditMock;
    private Mock<IUserRepository>            _userRepoMock;
    private TableService                     _tableService;

    private const int    TableId   = 1;
    private const int    WaiterId  = 10;
    private const string Qr        = "qr-001";

    [SetUp]
    public void SetUp()
    {
        _tableRepoMock   = new Mock<IRestaurentTableRepository>();
        _sessionRepoMock = new Mock<IDiningSessionRepository>();
        _auditMock       = new Mock<IAuditService>();
        _userRepoMock    = new Mock<IUserRepository>();

        _auditMock
            .Setup(a => a.LogAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuditAction>(),
                It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        _tableRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);
        _tableRepoMock
            .Setup(r => r.Create(It.IsAny<RestaurantTable>()))
            .Returns<RestaurantTable>(t => Task.FromResult(t));

        _tableService = new TableService(
            _tableRepoMock.Object,
            NullLogger<ITableService>.Instance,
            _sessionRepoMock.Object,
            _auditMock.Object,
            _userRepoMock.Object);
    }

    private static RestaurantTable AvailableTable() => new()
    {
        Id               = TableId,
        TableNumber      = "T1",
        QrIdentifier     = Qr,
        Capacity         = 4,
        Status           = TableStatus.Available,
        IsDeleted        = false,
        AssignedWaiterId = WaiterId,
        DiningSessions   = new List<DiningSession>()
    };


    [Test]
    public async Task GetAssignedTablesAsync_StatusMapping()
    {
        var unavailableTable = new RestaurantTable
        {
            Id = 1, TableNumber = "T1", Status = TableStatus.Unavailable,
            DiningSessions = new List<DiningSession>()
        };
        var occupiedTable = new RestaurantTable
        {
            Id = 2, TableNumber = "T2", Status = TableStatus.Available,
            DiningSessions = new List<DiningSession>
            {
                new() { Status = DiningSessionStatus.Active }
            }
        };
        var availableTable = new RestaurantTable
        {
            Id = 3, TableNumber = "T3", Status = TableStatus.Available,
            DiningSessions = new List<DiningSession>()
        };

        _tableRepoMock
            .Setup(r => r.GetAssignedTablesWithSessions(WaiterId))
            .ReturnsAsync(new List<RestaurantTable> { unavailableTable, occupiedTable, availableTable });

        var result = (await _tableService.GetAssignedTablesAsync(WaiterId)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result[0].Status, Is.EqualTo("Unavailable"));
            Assert.That(result[1].Status, Is.EqualTo("Occupied"));
            Assert.That(result[2].Status, Is.EqualTo("Available"));
        });
    }


    [Test]
    public async Task GetTableStatus_Unavailable()
    {
        var table = AvailableTable();
        table.Status = TableStatus.Unavailable;
        _tableRepoMock.Setup(r => r.GetByQrIdentifier(Qr)).ReturnsAsync(table);

        var result = await _tableService.GetTableStatus(Qr);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsAvailable,      Is.False);
            Assert.That(result.HasActiveSession, Is.False);
        });
    }

    [Test]
    public async Task GetTableStatus_Occupied()
    {
        _tableRepoMock.Setup(r => r.GetByQrIdentifier(Qr)).ReturnsAsync(AvailableTable());
        _sessionRepoMock
            .Setup(r => r.GetActiveSessionByTableId(TableId))
            .ReturnsAsync(new DiningSession { Id = 20, Status = DiningSessionStatus.Active });

        var result = await _tableService.GetTableStatus(Qr);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsAvailable,      Is.True);
            Assert.That(result.HasActiveSession, Is.True);
        });
    }

    [Test]
    public async Task GetTableStatus_Available()
    {
        _tableRepoMock.Setup(r => r.GetByQrIdentifier(Qr)).ReturnsAsync(AvailableTable());
        _sessionRepoMock
            .Setup(r => r.GetActiveSessionByTableId(TableId))
            .ReturnsAsync((DiningSession?)null);

        var result = await _tableService.GetTableStatus(Qr);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsAvailable,      Is.True);
            Assert.That(result.HasActiveSession, Is.False);
        });
    }

    [Test]
    public void GetTableStatus_TableNotFound()
    {
        _tableRepoMock.Setup(r => r.GetByQrIdentifier(Qr)).ReturnsAsync((RestaurantTable?)null);

        Assert.ThrowsAsync<TableNotFoundException>(() => _tableService.GetTableStatus(Qr));
    }


    [Test]
    public async Task GetTableDashboard_CountsCorrect()
    {
        var tables = new List<RestaurantTable>
        {
            new() { Id = 1, TableNumber = "T1", Status = TableStatus.Available  }, // will be occupied
            new() { Id = 2, TableNumber = "T2", Status = TableStatus.Available  }, // available
            new() { Id = 3, TableNumber = "T3", Status = TableStatus.Unavailable }  // unavailable
        };

        _tableRepoMock.Setup(r => r.GetAllNonDeletedTables()).ReturnsAsync(tables);
        // Table 1 has an active session
        _sessionRepoMock.Setup(r => r.GetActiveTableIds()).ReturnsAsync(new List<int> { 1 });

        var result = await _tableService.GetTableDashboard();

        Assert.Multiple(() =>
        {
            Assert.That(result.Summary.TotalTables,       Is.EqualTo(3));
            Assert.That(result.Summary.OccupiedTables,    Is.EqualTo(1));
            Assert.That(result.Summary.AvailableTables,   Is.EqualTo(1));
            Assert.That(result.Summary.UnavailableTables, Is.EqualTo(1));
        });
    }


    [Test]
    public async Task UpdateTableAvailability_Success()
    {
        var table = AvailableTable(); // Status = Available
        _tableRepoMock.Setup(r => r.Get(TableId)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.HasActiveSession(TableId)).ReturnsAsync(false);

        await _tableService.UpdateTableAvailability(TableId, TableStatus.Unavailable);

        Assert.That(table.Status, Is.EqualTo(TableStatus.Unavailable));
        _tableRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public void UpdateTableAvailability_ActiveSessionExists()
    {
        _tableRepoMock.Setup(r => r.Get(TableId)).ReturnsAsync(AvailableTable());
        _sessionRepoMock.Setup(r => r.HasActiveSession(TableId)).ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<Exception>(
            () => _tableService.UpdateTableAvailability(TableId, TableStatus.Unavailable));

        Assert.That(ex!.Message, Is.EqualTo("Cannot update availability during active dining session"));
    }

   

    [Test]
    public async Task DeleteTable_Success()
    {
        var table = AvailableTable();
        _tableRepoMock.Setup(r => r.Get(TableId)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.HasActiveSession(TableId)).ReturnsAsync(false);

        await _tableService.DeleteTable(TableId);

        Assert.That(table.IsDeleted, Is.True);
        _tableRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public void DeleteTable_ActiveSessionExists()
    {
        _tableRepoMock.Setup(r => r.Get(TableId)).ReturnsAsync(AvailableTable());
        _sessionRepoMock.Setup(r => r.HasActiveSession(TableId)).ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<Exception>(() => _tableService.DeleteTable(TableId));

        Assert.That(ex!.Message, Is.EqualTo("Cannot delete table with active dining session"));
    }



    [Test]
    public async Task AddTable_Success_WithWaiter()
    {
        _tableRepoMock.Setup(r => r.GetByTableNumber("T2")).ReturnsAsync((RestaurantTable?)null);
        _userRepoMock.Setup(r => r.GetActiveWaiter(WaiterId))
                     .ReturnsAsync(new User { Id = WaiterId, Role = UserRole.Waiter });

        var result = await _tableService.AddTable(new AddTableRequestDto
        {
            TableNumber      = "T2",
            Capacity         = 4,
            AssignedWaiterId = WaiterId
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.TableNumber,      Is.EqualTo("T2"));
            Assert.That(result.AssignedWaiterId, Is.EqualTo(WaiterId));
            Assert.That(result.Status,           Is.EqualTo(TableStatus.Available));
        });
        _tableRepoMock.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));
    }

    [Test]
    public async Task AddTable_Success_WithoutWaiter()
    {
        _tableRepoMock.Setup(r => r.GetByTableNumber("T3")).ReturnsAsync((RestaurantTable?)null);

        var result = await _tableService.AddTable(new AddTableRequestDto
        {
            TableNumber      = "T3",
            Capacity         = 2,
            AssignedWaiterId = null
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.AssignedWaiterId, Is.Null);
            Assert.That(result.Status,           Is.EqualTo(TableStatus.Unavailable));
        });
    }

    [Test]
    public void AddTable_WaiterNotFound()
    {
        _tableRepoMock.Setup(r => r.GetByTableNumber("T2")).ReturnsAsync((RestaurantTable?)null);
        _userRepoMock.Setup(r => r.GetActiveWaiter(WaiterId)).ReturnsAsync((User?)null);

        Assert.ThrowsAsync<WaiterNotFoundException>(
            () => _tableService.AddTable(new AddTableRequestDto
            {
                TableNumber      = "T2",
                Capacity         = 4,
                AssignedWaiterId = WaiterId
            }));
    }


    [Test]
    public async Task GetTableDetails_Success()
    {
        var waiter  = new User { Id = WaiterId, Name = "John" };
        var session = new DiningSession
        {
            Id         = 20,
            Status     = DiningSessionStatus.Active,
            StartedAt  = DateTime.UtcNow.AddMinutes(-30)
        };
        var table = new RestaurantTable
        {
            Id               = TableId,
            TableNumber      = "T1",
            AssignedWaiterId = WaiterId,
            AssignedWaiter   = waiter,
            DiningSessions   = new List<DiningSession> { session }
        };

        _tableRepoMock.Setup(r => r.GetTableDetails(TableId)).ReturnsAsync(table);

        var result = await _tableService.GetTableDetails(TableId);

        Assert.Multiple(() =>
        {
            Assert.That(result.TableId,            Is.EqualTo(TableId));
            Assert.That(result.AssignedWaiterName, Is.EqualTo("John"));
            Assert.That(result.Status,             Is.EqualTo("Occupied"));
            Assert.That(result.SessionStartedAt,   Is.Not.Null);
        });
    }

    [Test]
    public void GetTableDetails_NoActiveSession()
    {
        var table = new RestaurantTable
        {
            Id             = TableId,
            TableNumber    = "T1",
            DiningSessions = new List<DiningSession>() // no active sessions
        };

        _tableRepoMock.Setup(r => r.GetTableDetails(TableId)).ReturnsAsync(table);

        Assert.ThrowsAsync<SessionNotFoundException>(
            () => _tableService.GetTableDetails(TableId));
    }
}
