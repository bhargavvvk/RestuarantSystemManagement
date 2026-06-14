using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;

namespace RestaurantAPI.Repositories;

public class DiningSessionRepository:AbstractRepository<int,DiningSession>,IDiningSessionRepository
{
    public DiningSessionRepository(RestaurantContext context) : base(context)
    {

    }
    public async Task<DiningSession?> GetActiveSessionByTableId(int tableId)
    {
        return await _context.DiningSessions.FirstOrDefaultAsync(ds=>ds.TableId==tableId && ds.Status==DiningSessionStatus.Active);
    }
    public async Task<DiningSession?> GetActiveSessionByOtp(string otp)
    {
        return await _context.DiningSessions.FirstOrDefaultAsync(ds =>ds.SessionOtp == otp &&ds.Status == DiningSessionStatus.Active);
    }
    public async Task<DiningSession?> GetActiveSessionWithCartByTableId(int tableId)
    {
        return await _context.DiningSessions.Include(ds => ds.Cart)
                                            .FirstOrDefaultAsync(ds =>
                                                ds.TableId == tableId &&
                                                ds.Status == DiningSessionStatus.Active);
    }
    public async Task<ICollection<int>> GetActiveTableIds()
    {
        return await _context.DiningSessions
            .Where(ds => ds.Status == DiningSessionStatus.Active)
            .Select(ds => ds.TableId)
            .ToListAsync();
    }
    public async Task<bool> HasActiveSession(int tableId)
    {
        return await _context.DiningSessions
            .AnyAsync(ds =>
                ds.TableId == tableId &&
                ds.Status == DiningSessionStatus.Active);
    }
}
