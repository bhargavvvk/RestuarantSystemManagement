using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;

namespace RestaurantAPI.Repositories;

public class OrderRepository : AbstractRepository<int, Order>, IOrderRepository
{
    public OrderRepository(RestaurantContext context) : base(context)
    {
    }
    public async Task<ICollection<Order>> GetBySessionId(int sessionId)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .Where(o =>
                o.DiningSessionId == sessionId &&
                o.CancelledAt == null)
            .OrderByDescending(o => o.PlacedAt)
            .ToListAsync();
        }
    public async Task<int> GetKitchenQueueCount()
    {
        return await _context.Orders.CountAsync(o =>o.OrderItems!.All(i =>i.Status == OrderItemStatus.Placed));
    }
    public async Task<int> GetKitchenPreparingCount()
    {
        return await _context.Orders.CountAsync(o =>o.OrderItems!.Any(i =>i.Status == OrderItemStatus.Preparing));
    }
    public async Task<int> GetKitchenReadyCount()
    {
        return await _context.Orders.CountAsync(o =>o.OrderItems!.All(i =>i.Status == OrderItemStatus.Ready));
    }
    public async Task<IEnumerable<Order>> GetKitchenOrders(OrderItemStatus status)
    {
        var query = _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.DiningSession)
                .ThenInclude(ds => ds.Table)
            .AsQueryable();

        switch (status)
        {
            case OrderItemStatus.Placed:

                query = query.Where(o =>
                    o.OrderItems!.All(i =>
                        i.Status == OrderItemStatus.Placed));
                break;

            case OrderItemStatus.Preparing:

                query = query.Where(o =>
                    o.OrderItems!.Any(i =>
                        i.Status == OrderItemStatus.Preparing));
                break;

            case OrderItemStatus.Ready:

                query = query.Where(o =>
                    o.OrderItems!.Any(i => i.Status == OrderItemStatus.Ready) &&
                    o.OrderItems!.All(i =>
                        i.Status == OrderItemStatus.Ready ||
                        i.Status == OrderItemStatus.Served));
                break;
        }

        return await query
            .OrderBy(o => o.PlacedAt)
            .ToListAsync();
    }
    public async Task<int> GetTodayOrderCount()
    {
        var today = DateTime.Now.Date;

        return await _context.Orders
            .CountAsync(o => o.PlacedAt.Date == today);
    }
    public async Task<Order?> GetOrderWithItems(int orderId)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.DiningSession)
            .ThenInclude(d=>d.Table)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }
    public IQueryable<Order> GetOrdersQuery()
    {
        return _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.DiningSession)
                .ThenInclude(ds => ds!.Table)
            .AsQueryable();
    }
    public async Task<Order?> GetOrderDetails(int orderId)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.DiningSession)
                .ThenInclude(ds => ds.Table)
            .Include(o => o.DiningSession)
                .ThenInclude(ds => ds.Bill)
            .FirstOrDefaultAsync(
                o => o.Id == orderId);
    }
    public async Task<string?> GetLatestOrderNumberToday()
    {
        var today = DateTime.Today;
        return await _context.Orders
            .Where(o => o.PlacedAt.Date == today)
            .OrderByDescending(o => o.OrderNumber)
            .Select(o => o.OrderNumber)
            .FirstOrDefaultAsync();
    }
}
