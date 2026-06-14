using RestaurantAPI.Models;

namespace RestaurantAPI.RepositoryInterfaces;

public interface ICartItemRepository:IRepository<int,CartItem>
{
    Task<ICollection<CartItem>> GetByCartId(int cartId);
    Task<CartItem?> GetByCartAndMenuItem(int cartId, int menuItemId);
    Task<CartItem?> GetWithMenuItem(int cartItemId);
}
