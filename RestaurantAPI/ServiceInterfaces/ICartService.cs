using RestaurantAPI.Models.DTOs;

namespace RestaurantAPI.ServiceInterfaces;

public interface ICartService
{
   Task<ICollection<CartItemResponseDto>> GetCartItems(int cartId);
    Task AddToCart(int cartId,AddToCartDto request);
    Task UpdateCartItem(int cartId,int cartItemId,UpdateCartItemDto request);
    Task RemoveCartItem(  int cartId,int cartItemId);
}
