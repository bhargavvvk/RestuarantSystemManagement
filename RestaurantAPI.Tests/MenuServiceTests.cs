using AutoMapper;
using Microsoft.AspNetCore.Hosting;
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


public class MenuServiceTests
{
    private Mock<IMenuItemRepository>  _menuItemRepoMock;
    private Mock<ICategoryRepository>  _categoryRepoMock;
    private Mock<IMapper>              _mapperMock;
    private Mock<IAuditService>        _auditMock;
    private Mock<IWebHostEnvironment>  _webHostEnvMock;
    private RestaurantContext          _context;
    private MenuService                _menuService;

    [SetUp]
    public void SetUp()
    {
        _menuItemRepoMock = new Mock<IMenuItemRepository>();
        _categoryRepoMock = new Mock<ICategoryRepository>();
        _mapperMock       = new Mock<IMapper>();
        _auditMock        = new Mock<IAuditService>();
        _webHostEnvMock   = new Mock<IWebHostEnvironment>();

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

        _menuService = new MenuService(
            _menuItemRepoMock.Object,
            NullLogger<MenuService>.Instance,
            _mapperMock.Object,
            _auditMock.Object,
            _categoryRepoMock.Object,
            _context,
            _webHostEnvMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    private static Category ActiveCategory(int id = 1) =>
        new() { Id = id, Name = "Mains", IsAvailable = true, IsDeleted = false, MenuItems = new List<MenuItem>() };

    private static MenuItem SomeMenuItem(int id = 1) =>
        new() { Id = id, Name = "Burger", Price = 9.99m, IsAvailable = true, IsDeleted = false };

    private void SetUpSaveChanges()
    {
        _menuItemRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);
        _categoryRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);
    }


    [Test]
    public async Task AddCategory_success()
    {
        _categoryRepoMock.Setup(r => r.GetByName("Mains")).ReturnsAsync((Category?)null);
        _categoryRepoMock.Setup(r => r.Create(It.IsAny<Category>())).Returns<Category>(c => Task.FromResult(c));
        SetUpSaveChanges();

        var expected = new CategoryResponseDto { Id = 1, Name = "Mains", IsAvailable = true };
        _mapperMock.Setup(m => m.Map<CategoryResponseDto>(It.IsAny<Category>())).Returns(expected);

        var result = await _menuService.AddCategory(new AddCategoryDto { Name = "Mains", IsAvailable = true });

        Assert.That(result, Is.Not.Null);
        _categoryRepoMock.Verify(r => r.Create(It.IsAny<Category>()), Times.Once);
        _categoryRepoMock.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));
    }

    [Test]
    public void AddCategory_CategoryExists()
    {
        _categoryRepoMock.Setup(r => r.GetByName("Mains")).ReturnsAsync(ActiveCategory());

        var ex = Assert.ThrowsAsync<Exception>(
            () => _menuService.AddCategory(new AddCategoryDto { Name = "Mains" }));

        Assert.That(ex!.Message, Is.EqualTo("Category already exists"));
    }

    
    [Test]
    public async Task AddMenuItem_success()
    {
        _categoryRepoMock.Setup(r => r.Get(1)).ReturnsAsync(ActiveCategory());
        _menuItemRepoMock.Setup(r => r.GetByName("Burger")).ReturnsAsync((MenuItem?)null);
        _menuItemRepoMock.Setup(r => r.Create(It.IsAny<MenuItem>())).Returns<MenuItem>(m => Task.FromResult(m));
        SetUpSaveChanges();

        var expected = new MenuItemResponseDto { Id = 1, Name = "Burger", Price = 9.99m };
        _mapperMock.Setup(m => m.Map<MenuItemResponseDto>(It.IsAny<MenuItem>())).Returns(expected);

        var result = await _menuService.AddMenuItem(new AddMenuItemDto
        {
            Name       = "Burger",
            CategoryId = 1,
            Price      = 9.99m
        });

        Assert.That(result, Is.Not.Null);
        _menuItemRepoMock.Verify(r => r.Create(It.IsAny<MenuItem>()), Times.Once);
        _menuItemRepoMock.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));
    }

    [Test]
    public void AddMenuItem_CategoryNotFound()
    {
        _categoryRepoMock.Setup(r => r.Get(99)).ReturnsAsync((Category?)null);

        Assert.ThrowsAsync<CategoryNotFoundException>(
            () => _menuService.AddMenuItem(new AddMenuItemDto { Name = "Burger", CategoryId = 99, Price = 9.99m }));
    }

    [Test]
    public void AddMenuItem_ShouldThrow_WhenNameIsEmpty()
    {
        var ex = Assert.ThrowsAsync<Exception>(
            () => _menuService.AddMenuItem(new AddMenuItemDto { Name = "", CategoryId = 1, Price = 9.99m }));

        Assert.That(ex!.Message, Is.EqualTo("Menu item name is required"));
    }

    [Test]
    public void AddMenuItem_PriceIsZeroOrLess()
    {
        var ex = Assert.ThrowsAsync<Exception>(
            () => _menuService.AddMenuItem(new AddMenuItemDto { Name = "Burger", CategoryId = 1, Price = 0m }));

        Assert.That(ex!.Message, Is.EqualTo("Price must be greater than zero"));
    }

    [Test]
    public async Task UpdateMenuItem_Success()
    {
        var item = SomeMenuItem();

        _menuItemRepoMock.Setup(r => r.Get(1)).ReturnsAsync(item);
        _categoryRepoMock.Setup(r => r.Get(1)).ReturnsAsync(ActiveCategory());
        _menuItemRepoMock.Setup(r => r.GetByName("Pizza")).ReturnsAsync((MenuItem?)null);
        SetUpSaveChanges();

        var expected = new MenuItemResponseDto { Id = 1, Name = "Pizza", Price = 12m };
        _mapperMock.Setup(m => m.Map<MenuItemResponseDto>(It.IsAny<MenuItem>())).Returns(expected);

        var result = await _menuService.UpdateMenuItem(1, new UpdateMenuItemDto
        {
            Name       = "Pizza",
            Price      = 12m,
            CategoryId = 1
        });

        Assert.That(result, Is.Not.Null);
        _menuItemRepoMock.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));
    }

    [Test]
    public void UpdateMenuItem_MenuItemNotFound()
    {
        _menuItemRepoMock.Setup(r => r.Get(99)).ReturnsAsync((MenuItem?)null);

        Assert.ThrowsAsync<MenuItemNotFoundException>(
            () => _menuService.UpdateMenuItem(99, new UpdateMenuItemDto { Name = "Pizza", Price = 10m, CategoryId = 1 }));
    }

    [Test]
    public async Task DeleteMenuItem_Successful()
    {
        var item = SomeMenuItem();

        _menuItemRepoMock.Setup(r => r.Get(1)).ReturnsAsync(item);
        SetUpSaveChanges();

        await _menuService.DeleteMenuItem(1);

        Assert.Multiple(() =>
        {
            Assert.That(item.IsDeleted,   Is.True);
            Assert.That(item.IsAvailable, Is.False);
        });
        _menuItemRepoMock.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));
    }
    [Test]
    public async Task DeleteCategory_Successful()
    {
        var menuItem = SomeMenuItem();
        var category = ActiveCategory();
        category.MenuItems = new List<MenuItem> { menuItem };

        _categoryRepoMock.Setup(r => r.Get(1)).ReturnsAsync(category);
        SetUpSaveChanges();

        await _menuService.DeleteCategory(1);

        Assert.Multiple(() =>
        {
            Assert.That(category.IsDeleted,    Is.True);
            Assert.That(category.IsAvailable,  Is.False);
            Assert.That(menuItem.IsDeleted,    Is.True);
            Assert.That(menuItem.IsAvailable,  Is.False);
        });
        _categoryRepoMock.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));
    }

    [Test]
    public void DeleteCategory_CategoryNotFound()
    {
        _categoryRepoMock.Setup(r => r.Get(99)).ReturnsAsync((Category?)null);
        Assert.ThrowsAsync<CategoryNotFoundException>(
            () => _menuService.DeleteCategory(99));
    }

    [Test]
    public async Task GetMenu_Success()
    {
        
        var category = new Category { Name = "Mains", IsAvailable = true };
        _context.Set<Category>().Add(category);
        await _context.SaveChangesAsync();

        var items = new List<MenuItem>
        {
            new() { Name = "Burger", Price = 9.99m, IsAvailable = true, CategoryId = category.Id, FoodType = FoodType.Veg },
            new() { Name = "Pizza",  Price = 12m,   IsAvailable = true, CategoryId = category.Id, FoodType = FoodType.Veg }
        };
        _context.Set<MenuItem>().AddRange(items);
        await _context.SaveChangesAsync();

        _menuItemRepoMock
            .Setup(r => r.GetMenuQuery())
            .Returns(_context.Set<MenuItem>().Where(m => !m.IsDeleted));

        var expected = items.Select(i => new MenuItemResponseDto { Id = i.Id, Name = i.Name }).ToList();
        _mapperMock
            .Setup(m => m.Map<ICollection<MenuItemResponseDto>>(It.IsAny<object>()))
            .Returns(expected);

        var result = await _menuService.GetMenu(null, null, null, null);
        Assert.That(result.Count, Is.EqualTo(2));
    }
}
