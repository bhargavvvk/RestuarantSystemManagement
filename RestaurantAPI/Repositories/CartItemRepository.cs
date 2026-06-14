using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;

namespace RestaurantAPI.Repositories;

public class CartItemRepository:AbstractRepository<int,CartItem>,ICartItemRepository
{
    public CartItemRepository(RestaurantContext context) : base(context)
    {
    }
    public async Task<CartItem?> GetByCartAndMenuItem(int cartId,int menuItemId)
    {
        return await _context.CartItems.FirstOrDefaultAsync(ci =>ci.CartId == cartId &&ci.MenuItemId == menuItemId);
    }

    public async Task<ICollection<CartItem>> GetByCartId(int cartId)
    {
        return await _context.CartItems
            .Include(ci => ci.MenuItem)
            .Where(ci => ci.CartId == cartId)
            .ToListAsync();
    }

    public async Task<CartItem?> GetWithMenuItem(int cartItemId)
    {
        return await _context.CartItems.Include(ci => ci.MenuItem).FirstOrDefaultAsync(ci => ci.Id == cartItemId);
    }
    public override async Task<CartItem?> Delete(int key)
    {
       var cartItem =await Get(key);
        if(cartItem == null)
        {
            return null;
        }
        _context.CartItems.Remove(cartItem);
        return cartItem;
    }
}
