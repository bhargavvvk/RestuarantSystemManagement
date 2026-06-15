using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Services;

public class CartService : ICartService
{
   private readonly ICartItemRepository _cartItemRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICartRepository _cartRepository;
    private readonly ILogger<CartService> _logger;
    private readonly IBillRepository _billRepository;
    public CartService(ICartItemRepository cartItemRepository, IMenuItemRepository menuItemRepository, ILogger<CartService> logger, ICartRepository cartRepository,
    IBillRepository billRepository)
    {
        _cartItemRepository = cartItemRepository;
        _menuItemRepository=menuItemRepository;
        _logger = logger;
        _cartRepository = cartRepository;
        _billRepository=billRepository;
    }
    public async Task RemoveCartItem(int cartId, int cartItemId)
    {
        await ValidateBillNotPaid(cartId);
        var cartItem =await _cartItemRepository.Get(cartItemId);
        if(cartItem == null)
        {
            throw new CartItemNotFoundException();
        }
        if(cartItem.CartId != cartId)
        {
            throw new UnauthorizedAccessException("cart not belongs to user");
        }
        await _cartItemRepository.Delete(cartItemId);
        await _cartItemRepository.SaveChangesAsync();
        _logger.LogInformation("Cart item {CartItemId} removed",cartItemId);
    }

    public async Task UpdateCartItem(int cartId,int cartItemId,UpdateCartItemDto request)
    {
        await ValidateBillNotPaid(cartId);
        var cartItem =await _cartItemRepository.GetWithMenuItem(cartItemId);
        if(cartItem == null)
        {
            throw new CartItemNotFoundException();
        }
        if(cartItem.CartId != cartId)
        {
            throw new UnauthorizedAccessException("cart not belongs to user");
        }
        if(!cartItem.MenuItem!.IsAvailable)
        {
            throw new MenuItemNotFoundException();
        }

        if(request.Quantity <= 0)
        {
            throw new CartException("Quantity must be greather than 0");
        }
        cartItem.Quantity = request.Quantity;
        await _cartItemRepository.Update(cartItem.Id,cartItem);
        await _cartItemRepository.SaveChangesAsync();
        _logger.LogInformation(
            "Cart item {CartItemId} updated to quantity {Quantity}",
            cartItemId,
            request.Quantity);
    }
    public async Task<ICollection<CartItemResponseDto>>GetCartItems(int cartId)
    {
        var cartItems =
            await _cartItemRepository.GetByCartId(cartId);
        _logger.LogInformation("Retrieved {ItemCount} cart items for cart {CartId}", cartItems.Count, cartId);
        return cartItems
            .Select(ci =>
                new CartItemResponseDto
                {
                    Id = ci.Id,
                    MenuItemId = ci.MenuItemId,
                    MenuItemName = ci.MenuItem!.Name,
                    Quantity = ci.Quantity,
                    Price = ci.MenuItem.Price,
                    IsAvailable = ci.MenuItem.IsAvailable
                })
            .ToList();
    }
    public async Task AddToCart(int cartId, AddToCartDto request)
    {   await ValidateBillNotPaid(cartId);
        var menuItem =await _menuItemRepository.Get(request.MenuItemId);
        if(menuItem == null)
        {
            throw new MenuItemNotFoundException();
        }

        if(!menuItem.IsAvailable)
        {
            throw new MenuItemUnavailableException();
        }
        var existingCartItem =await _cartItemRepository.GetByCartAndMenuItem(cartId,request.MenuItemId);
        if(existingCartItem != null)
        {
            throw new CartException("Item Already Exists in the cart");
        }

        var cartItem = new CartItem
        {
            CartId = cartId,
            MenuItemId = request.MenuItemId,
            Quantity = 1
        };
        await _cartItemRepository.Create(cartItem);
        await _cartItemRepository.SaveChangesAsync();
        _logger.LogInformation("Menu item {MenuItemId} added to cart {CartId}",request.MenuItemId,cartId);
    }
    private async Task ValidateBillNotPaid(int cartId)
    {
        var cart = await _cartRepository.Get(cartId);
        if (cart == null)
        {
            throw new CartNotFoundException();
        }
        var bill = await _billRepository.GetBySessionId(cart.DiningSessionId);

        if (bill == null)
        {
            throw new BillNotFoundException();
        }

        if (bill.PaymentStatus == PaymentStatus.Paid)
        {
            throw new CartException("Cannot modify cart after bill payment.");
        }
    }
}
