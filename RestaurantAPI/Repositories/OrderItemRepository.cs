using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;

namespace RestaurantAPI.Repositories;

public class OrderItemRepository : AbstractRepository<int, OrderItem>, IOrderItemRepository
{
    public OrderItemRepository(RestaurantContext context) : base(context)
    {
    }
    public async Task<OrderItem?> GetOrderItemWithOrderAsync(int orderItemId)
    {
        return await _context.OrderItems
            .Include(oi => oi.Order)
            .FirstOrDefaultAsync(oi =>
                oi.Id == orderItemId);
    }
    public async Task<IEnumerable<OrderItem>> GetKitchenOrderItems(OrderItemStatus status)
    {
        return await _context.OrderItems
            .Include(oi => oi.Order)
                .ThenInclude(o => o.DiningSession)
                    .ThenInclude(ds => ds.Table)
            .Where(oi => oi.Status == status)
            .OrderBy(oi => oi.Order!.PlacedAt)
            .ToListAsync();
    }
    public async Task<ICollection<OrderItem>> GetActiveOrderItemsBySessionId(int sessionId)
    {
        return await _context.OrderItems
            .Include(oi => oi.Order)
            .Where(oi =>
                oi.Order!.DiningSessionId == sessionId &&
                (oi.Status == OrderItemStatus.Placed ||
                oi.Status == OrderItemStatus.Preparing ||
                oi.Status == OrderItemStatus.Ready))
            .ToListAsync();
    }
}
