using Microsoft.EntityFrameworkCore;
using Moq;
using RestaurantAPI.Contexts;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;
using RestaurantAPI.Services;

namespace RestaurantAPI.Tests;

public class InventoryServiceTests
{
    private Mock<IInventoryItemRepository> _inventoryRepoMock;
    private Mock<IAuditService>            _auditMock;
    private RestaurantContext              _context;
    private InventoryService               _inventoryService;

    [SetUp]
    public void SetUp()
    {
        _inventoryRepoMock = new Mock<IInventoryItemRepository>();
        _auditMock         = new Mock<IAuditService>();

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

        _inventoryService = new InventoryService(
            _inventoryRepoMock.Object,
            _context,
            _auditMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }


    [Test]
    public async Task AddInventoryItem_Success()
    {
        // Arrange
        _inventoryRepoMock.Setup(r => r.GetByName("Rice")).ReturnsAsync((InventoryItem?)null);
        _inventoryRepoMock.Setup(r => r.Create(It.IsAny<InventoryItem>()))
                          .Returns<InventoryItem>(i => Task.FromResult(i));
        _inventoryRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        var request = new AddInventoryItemDto
        {
            ItemName          = "Rice",
            Unit              = "Kg",
            CurrentQuantity   = 50m,
            ThresholdQuantity = 10m
        };

        // Act
        await _inventoryService.AddInventoryItem(request);

        // Assert
        _inventoryRepoMock.Verify(r => r.Create(It.Is<InventoryItem>(i =>
            i.Name == "Rice" &&
            i.Unit == "Kg"   &&
            i.AvailableQuantity == 50m &&
            i.MinimumStockThreshold == 10m)), Times.Once);

        _inventoryRepoMock.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));

        _auditMock.Verify(a => a.LogAsync(
            nameof(InventoryItem),
            It.IsAny<string>(),
            AuditAction.Created,
            null,
            It.IsAny<object>(),
            "Inventory item created"), Times.Once);
    }

    [Test]
    public void AddInventoryItem_EmptyName_ThrowsException()
    {
        var request = new AddInventoryItemDto
        {
            ItemName          = "   ",
            Unit              = "Kg",
            CurrentQuantity   = 10m,
            ThresholdQuantity = 5m
        };

        var ex = Assert.ThrowsAsync<Exception>(() => _inventoryService.AddInventoryItem(request));
        Assert.That(ex!.Message, Is.EqualTo("Item name is required"));
    }

    [Test]
    public void AddInventoryItem_EmptyUnit_ThrowsException()
    {
        var request = new AddInventoryItemDto
        {
            ItemName          = "Rice",
            Unit              = "",
            CurrentQuantity   = 10m,
            ThresholdQuantity = 5m
        };

        var ex = Assert.ThrowsAsync<Exception>(() => _inventoryService.AddInventoryItem(request));
        Assert.That(ex!.Message, Is.EqualTo("Unit is required"));
    }

    [Test]
    public void AddInventoryItem_NegativeQuantity_ThrowsException()
    {
        var request = new AddInventoryItemDto
        {
            ItemName          = "Rice",
            Unit              = "Kg",
            CurrentQuantity   = -1m,
            ThresholdQuantity = 5m
        };

        var ex = Assert.ThrowsAsync<Exception>(() => _inventoryService.AddInventoryItem(request));
        Assert.That(ex!.Message, Is.EqualTo("Quantity cannot be negative"));
    }

    [Test]
    public void AddInventoryItem_NegativeThreshold_ThrowsException()
    {
        var request = new AddInventoryItemDto
        {
            ItemName          = "Rice",
            Unit              = "Kg",
            CurrentQuantity   = 10m,
            ThresholdQuantity = -1m
        };

        var ex = Assert.ThrowsAsync<Exception>(() => _inventoryService.AddInventoryItem(request));
        Assert.That(ex!.Message, Is.EqualTo("Threshold quantity cannot be negative"));
    }

    [Test]
    public void AddInventoryItem_DuplicateName_ThrowsDuplicateEntityException()
    {
        _inventoryRepoMock.Setup(r => r.GetByName("Rice"))
                          .ReturnsAsync(new InventoryItem { Name = "Rice" });

        var request = new AddInventoryItemDto
        {
            ItemName          = "Rice",
            Unit              = "Kg",
            CurrentQuantity   = 10m,
            ThresholdQuantity = 5m
        };

        Assert.ThrowsAsync<DuplicateEntityException>(
            () => _inventoryService.AddInventoryItem(request));
    }


    [Test]
    public async Task UpdateInventoryQuantity_Success()
    {
        var item = new InventoryItem { Id = 1, Name = "Rice", AvailableQuantity = 50m, Unit = "Kg", MinimumStockThreshold = 10m };

        _inventoryRepoMock.Setup(r => r.Get(1)).ReturnsAsync(item);
        _inventoryRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        // Act
        await _inventoryService.UpdateInventoryQuantity(1, 30m);

       
        Assert.That(item.AvailableQuantity, Is.EqualTo(30m));

        _inventoryRepoMock.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));

        _auditMock.Verify(a => a.LogAsync(
            nameof(InventoryItem),
            "1",
            AuditAction.Updated,
            It.IsAny<object>(),
            It.IsAny<object>(),
            "Inventory quantity updated"), Times.Once);
    }

    [Test]
    public async Task UpdateInventoryThreshold_Success_UpdatesAndLogsAudit()
    {
        var item = new InventoryItem { Id = 2, Name = "Paneer", AvailableQuantity = 20m, Unit = "Kg", MinimumStockThreshold = 5m };

        _inventoryRepoMock.Setup(r => r.Get(2)).ReturnsAsync(item);
        _inventoryRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        // Act
        await _inventoryService.UpdateInventoryThreshold(2, 8m);

        // Assert
        Assert.That(item.MinimumStockThreshold, Is.EqualTo(8m));

        _auditMock.Verify(a => a.LogAsync(
            nameof(InventoryItem),
            "2",
            AuditAction.Updated,
            It.IsAny<object>(),
            It.IsAny<object>(),
            "Inventory threshold updated"), Times.Once);
    }

    [Test]
    public async Task DeleteInventoryItem_Success_SetsIsDeletedAndLogsAudit()
    {
        var item = new InventoryItem { Id = 3, Name = "Chicken", IsDeleted = false };

        _inventoryRepoMock.Setup(r => r.Get(3)).ReturnsAsync(item);
        _inventoryRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        // Act
        await _inventoryService.DeleteInventoryItem(3);

        // Assert
        Assert.That(item.IsDeleted, Is.True);

        _inventoryRepoMock.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));

        _auditMock.Verify(a => a.LogAsync(
            nameof(InventoryItem),
            "3",
            AuditAction.Deleted,
            It.IsAny<object>(),
            It.IsAny<object>(),
            It.Is<string>(s => s.Contains("Chicken"))), Times.Once);
    }
}
