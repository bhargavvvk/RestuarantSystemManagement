using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;

namespace RestaurantAPI.Repositories;

public class CartRepository:AbstractRepository<int,Cart>,ICartRepository
{
    public CartRepository(RestaurantContext context):base(context)
    {

    }

    public async Task<Cart?> GetByDiningSessionId(int diningSessionId)
    {
        return await _context.Carts.FirstOrDefaultAsync(c =>c.DiningSessionId == diningSessionId);
    }
}
