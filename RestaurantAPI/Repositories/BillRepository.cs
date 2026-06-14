using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;

namespace RestaurantAPI.Repositories;

public class BillRepository : AbstractRepository<int, Bill>, IBillRepository
{
    public BillRepository(RestaurantContext context) : base(context)
    {

    }
    public async Task<int> GetBillCount()
    {
        return await _context.Bills.CountAsync();
    }

    public async Task<Bill?> GetBySessionId(int sessionId)
    {
        return await _context.Bills.Include(b=>b.TaxConfiguration).FirstOrDefaultAsync(b=>b.DiningSessionId==sessionId);
    }
    public IQueryable<Bill> GetBillsQuery()
    {
        return _context.Bills
            .Include(b => b.DiningSession)
                .ThenInclude(ds => ds!.Table)
            .AsQueryable();
    }
    public async Task<Bill?> GetBillDetails(int billId)
    {
        return await _context.Bills
            .Include(b => b.TaxConfiguration)
            .Include(b => b.DiningSession)
                .ThenInclude(ds => ds.Table)
            .Include(b => b.DiningSession)
                .ThenInclude(ds => ds.Waiter)
            .FirstOrDefaultAsync(
                b => b.Id == billId);
    }
    public async Task<string?> GetLatestBillNumberToday()
    {
        var today = DateTime.Today;
        return await _context.Bills
            .Where(o => o.GeneratedAt.Date == today)
            .OrderByDescending(o => o.BillNumber)
            .Select(o => o.BillNumber)
            .FirstOrDefaultAsync();
    }
}
