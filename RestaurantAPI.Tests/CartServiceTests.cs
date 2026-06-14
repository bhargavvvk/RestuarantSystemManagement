using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.Services;

namespace RestaurantAPI.Tests;

public class CartServiceTests
{
    private Mock<ICartItemRepository> _cartItemRepoMock;
    private Mock<IMenuItemRepository> _menuItemRepoMock;
    private Mock<ICartRepository>     _cartRepoMock;
    private Mock<IBillRepository>     _billRepoMock;
    private CartService  _cartService;

    private const int CartId     = 10;
    private const int SessionId  = 20;
    private const int MenuItemId = 30;
    private const int CartItemId = 40;

    private Cart ActiveCart      => new() { Id = CartId, DiningSessionId = SessionId };
    private Bill PendingBill     => new() { Id = 1, DiningSessionId = SessionId, PaymentStatus = PaymentStatus.Pending };
    private Bill PaidBill        => new() { Id = 1, DiningSessionId = SessionId, PaymentStatus = PaymentStatus.Paid };
    private MenuItem AvailableItem   => new() { Id = MenuItemId, Name = "Burger", Price = 9.99m, IsAvailable = true };
    private MenuItem UnavailableItem => new() { Id = MenuItemId, Name = "Burger", Price = 9.99m, IsAvailable = false };

    [SetUp]
    public void SetUp()
    {
        _cartItemRepoMock = new Mock<ICartItemRepository>();
        _menuItemRepoMock = new Mock<IMenuItemRepository>();
        _cartRepoMock     = new Mock<ICartRepository>();
        _billRepoMock     = new Mock<IBillRepository>();

        _cartService = new CartService(
            _cartItemRepoMock.Object,
            _menuItemRepoMock.Object,
            NullLogger<CartService>.Instance,
            _cartRepoMock.Object,
            _billRepoMock.Object);
    }

    private void SetUpValidCart()
    {
        _cartRepoMock.Setup(r => r.Get(CartId)).ReturnsAsync(ActiveCart);
        _billRepoMock.Setup(r => r.GetBySessionId(SessionId)).ReturnsAsync(PendingBill);
    }

    [Test]
    public async Task AddToCart_PassTest()
    {
        SetUpValidCart();
        _menuItemRepoMock.Setup(r => r.Get(MenuItemId)).ReturnsAsync(AvailableItem);
        _cartItemRepoMock.Setup(r => r.GetByCartAndMenuItem(CartId, MenuItemId)).ReturnsAsync((CartItem?)null);
        _cartItemRepoMock.Setup(r => r.Create(It.IsAny<CartItem>())).Returns<CartItem>(c => Task.FromResult(c));
        _cartItemRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);
        await _cartService.AddToCart(CartId, new AddToCartDto { MenuItemId = MenuItemId });
        _cartItemRepoMock.Verify(r => r.Create(It.IsAny<CartItem>()), Times.Once);
        _cartItemRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public void AddToCart_CartNotFound()
    {
        _cartRepoMock.Setup(r => r.Get(CartId)).ReturnsAsync((Cart?)null);

        Assert.ThrowsAsync<CartNotFoundException>(
            () => _cartService.AddToCart(CartId, new AddToCartDto { MenuItemId = MenuItemId }));
    }

    [Test]
    public void AddToCart_BillNotFound()
    {
        _cartRepoMock.Setup(r => r.Get(CartId)).ReturnsAsync(ActiveCart);
        _billRepoMock.Setup(r => r.GetBySessionId(SessionId)).ReturnsAsync((Bill?)null);

        Assert.ThrowsAsync<BillNotFoundException>(
            () => _cartService.AddToCart(CartId, new AddToCartDto { MenuItemId = MenuItemId }));
    }

    [Test]
    public void AddToCart_BillAlreadyPaid()
    {
        _cartRepoMock.Setup(r => r.Get(CartId)).ReturnsAsync(ActiveCart);
        _billRepoMock.Setup(r => r.GetBySessionId(SessionId)).ReturnsAsync(PaidBill);

        var ex = Assert.ThrowsAsync<CartException>(
            () => _cartService.AddToCart(CartId, new AddToCartDto { MenuItemId = MenuItemId }));

        Assert.That(ex!.Message, Is.EqualTo("Cannot modify cart after bill payment."));
    }

    [Test]
    public void AddToCart_MenuItemNotFound()
    {
        SetUpValidCart();
        _menuItemRepoMock.Setup(r => r.Get(MenuItemId)).ReturnsAsync((MenuItem?)null);

        Assert.ThrowsAsync<MenuItemNotFoundException>(
            () => _cartService.AddToCart(CartId, new AddToCartDto { MenuItemId = MenuItemId }));
    }

    [Test]
    public void AddToCart_MenuItemUnavailable()
    {
        SetUpValidCart();
        _menuItemRepoMock.Setup(r => r.Get(MenuItemId)).ReturnsAsync(UnavailableItem);

        Assert.ThrowsAsync<MenuItemUnavailableException>(
            () => _cartService.AddToCart(CartId, new AddToCartDto { MenuItemId = MenuItemId }));
    }

    [Test]
    public void AddToCart_ItemAlreadyExists()
    {
        SetUpValidCart();
        _menuItemRepoMock.Setup(r => r.Get(MenuItemId)).ReturnsAsync(AvailableItem);
        _cartItemRepoMock.Setup(r => r.GetByCartAndMenuItem(CartId, MenuItemId))
                         .ReturnsAsync(new CartItem { Id = CartItemId, CartId = CartId, MenuItemId = MenuItemId });

        var ex = Assert.ThrowsAsync<CartException>(
            () => _cartService.AddToCart(CartId, new AddToCartDto { MenuItemId = MenuItemId }));

        Assert.That(ex!.Message, Is.EqualTo("Item Already Exists in the cart"));
    }

    [Test]
    public async Task UpdateCartItem_PassTest()
    {
        SetUpValidCart();

        var cartItem = new CartItem
        {
            Id         = CartItemId,
            CartId     = CartId,
            MenuItemId = MenuItemId,
            Quantity   = 1,
            MenuItem   = AvailableItem
        };

        _cartItemRepoMock.Setup(r => r.GetWithMenuItem(CartItemId)).ReturnsAsync(cartItem);
        _cartItemRepoMock.Setup(r => r.Update(CartItemId, cartItem)).ReturnsAsync(cartItem);
        _cartItemRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        await _cartService.UpdateCartItem(CartId, CartItemId, new UpdateCartItemDto { Quantity = 3 });

        _cartItemRepoMock.Verify(r => r.Update(CartItemId, It.IsAny<CartItem>()), Times.Once);
        _cartItemRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public void UpdateCartItem_CartItemNotFound()
    {
        SetUpValidCart();
        _cartItemRepoMock.Setup(r => r.GetWithMenuItem(CartItemId)).ReturnsAsync((CartItem?)null);

        Assert.ThrowsAsync<CartItemNotFoundException>(
            () => _cartService.UpdateCartItem(CartId, CartItemId, new UpdateCartItemDto { Quantity = 2 }));
    }

    [Test]
    public void UpdateCartItem_CartItemBelongsToAnotherCart()
    {
        SetUpValidCart();

        var cartItem = new CartItem
        {
            Id       = CartItemId,
            CartId   = 999,
            MenuItem = AvailableItem
        };

        _cartItemRepoMock.Setup(r => r.GetWithMenuItem(CartItemId)).ReturnsAsync(cartItem);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _cartService.UpdateCartItem(CartId, CartItemId, new UpdateCartItemDto { Quantity = 2 }));
    }

    [Test]
    public void UpdateCartItem_MenuItemUnavailable()
    {
        SetUpValidCart();

        var cartItem = new CartItem
        {
            Id         = CartItemId,
            CartId     = CartId,
            MenuItemId = MenuItemId,
            MenuItem   = UnavailableItem
        };

        _cartItemRepoMock.Setup(r => r.GetWithMenuItem(CartItemId)).ReturnsAsync(cartItem);

        Assert.ThrowsAsync<MenuItemNotFoundException>(
            () => _cartService.UpdateCartItem(CartId, CartItemId, new UpdateCartItemDto { Quantity = 2 }));
    }

    [Test]
    public void UpdateCartItem_QuantityLessThanOrEqualToZero()
    {
        SetUpValidCart();

        var cartItem = new CartItem
        {
            Id         = CartItemId,
            CartId     = CartId,
            MenuItemId = MenuItemId,
            MenuItem   = AvailableItem
        };

        _cartItemRepoMock.Setup(r => r.GetWithMenuItem(CartItemId)).ReturnsAsync(cartItem);

        var ex = Assert.ThrowsAsync<CartException>(
            () => _cartService.UpdateCartItem(CartId, CartItemId, new UpdateCartItemDto { Quantity = 0 }));

        Assert.That(ex!.Message, Is.EqualTo("Quantity must be greather than 0"));
    }

    [Test]
    public void UpdateCartItem_BillAlreadyPaid()
    {
        _cartRepoMock.Setup(r => r.Get(CartId)).ReturnsAsync(ActiveCart);
        _billRepoMock.Setup(r => r.GetBySessionId(SessionId)).ReturnsAsync(PaidBill);

        var ex = Assert.ThrowsAsync<CartException>(
            () => _cartService.UpdateCartItem(CartId, CartItemId, new UpdateCartItemDto { Quantity = 2 }));

        Assert.That(ex!.Message, Is.EqualTo("Cannot modify cart after bill payment."));
    }

    [Test]
    public async Task RemoveCartItem_PassTest()
    {
        SetUpValidCart();

        var cartItem = new CartItem { Id = CartItemId, CartId = CartId };
        _cartItemRepoMock.Setup(r => r.Get(CartItemId)).ReturnsAsync(cartItem);
        _cartItemRepoMock.Setup(r => r.Delete(CartItemId)).ReturnsAsync(cartItem);
        _cartItemRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        await _cartService.RemoveCartItem(CartId, CartItemId);

        _cartItemRepoMock.Verify(r => r.Delete(CartItemId), Times.Once);
        _cartItemRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public void RemoveCartItem_CartItemNotFound()
    {
        SetUpValidCart();
        _cartItemRepoMock.Setup(r => r.Get(CartItemId)).ReturnsAsync((CartItem?)null);

        Assert.ThrowsAsync<CartItemNotFoundException>(
            () => _cartService.RemoveCartItem(CartId, CartItemId));
    }

    [Test]
    public void RemoveCartItem_ItemBelongsToAnotherCart()
    {
        SetUpValidCart();

        var cartItem = new CartItem { Id = CartItemId, CartId = 999 }; // wrong cart
        _cartItemRepoMock.Setup(r => r.Get(CartItemId)).ReturnsAsync(cartItem);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _cartService.RemoveCartItem(CartId, CartItemId));
    }

    [Test]
    public void RemoveCartItem_BillAlreadyPaid()
    {
        _cartRepoMock.Setup(r => r.Get(CartId)).ReturnsAsync(ActiveCart);
        _billRepoMock.Setup(r => r.GetBySessionId(SessionId)).ReturnsAsync(PaidBill);

        var ex = Assert.ThrowsAsync<CartException>(
            () => _cartService.RemoveCartItem(CartId, CartItemId));

        Assert.That(ex!.Message, Is.EqualTo("Cannot modify cart after bill payment."));
    }

    [Test]
    public async Task GetCartItems_PassTest()
    {
        var menuItem = AvailableItem;
        var cartItems = new List<CartItem>
        {
            new()
            {
                Id         = CartItemId,
                CartId     = CartId,
                MenuItemId = MenuItemId,
                Quantity   = 2,
                MenuItem   = menuItem
            }
        };

        _cartItemRepoMock.Setup(r => r.GetByCartId(CartId)).ReturnsAsync(cartItems);

        var result = await _cartService.GetCartItems(CartId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Count,                     Is.EqualTo(1));
            Assert.That(result.First().Id,                Is.EqualTo(CartItemId));
            Assert.That(result.First().MenuItemId,        Is.EqualTo(MenuItemId));
            Assert.That(result.First().MenuItemName,      Is.EqualTo(menuItem.Name));
            Assert.That(result.First().Quantity,          Is.EqualTo(2));
            Assert.That(result.First().Price,             Is.EqualTo(menuItem.Price));
            Assert.That(result.First().IsAvailable,       Is.True);
        });
    }
}
