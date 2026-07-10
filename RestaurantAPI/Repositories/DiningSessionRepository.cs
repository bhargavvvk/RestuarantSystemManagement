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
        return await _context.DiningSessions
            .Include(ds => ds.DiningSessionTables)
            .FirstOrDefaultAsync(ds=> (ds.TableId==tableId || ds.DiningSessionTables.Any(dst => dst.TableId == tableId)) && ds.Status==DiningSessionStatus.Active);
    }
    public async Task<DiningSession?> GetActiveSessionByOtp(string otp)
    {
        return await _context.DiningSessions
            .Include(ds => ds.DiningSessionTables)
            .FirstOrDefaultAsync(ds =>ds.SessionOtp == otp &&ds.Status == DiningSessionStatus.Active);
    }
    public async Task<DiningSession?> GetActiveSessionWithCartByTableId(int tableId)
    {
        return await _context.DiningSessions.Include(ds => ds.Cart)
                                            .Include(ds => ds.DiningSessionTables)
                                            .FirstOrDefaultAsync(ds =>
                                                (ds.TableId == tableId || ds.DiningSessionTables.Any(dst => dst.TableId == tableId)) &&
                                                ds.Status == DiningSessionStatus.Active);
    }
    public async Task<ICollection<int>> GetActiveTableIds()
    {
        var primaryIds = await _context.DiningSessions
            .Where(ds => ds.Status == DiningSessionStatus.Active)
            .Select(ds => ds.TableId)
            .ToListAsync();
            
        var linkedIds = await _context.DiningSessionTables
            .Where(dst => dst.DiningSession!.Status == DiningSessionStatus.Active)
            .Select(dst => dst.TableId)
            .ToListAsync();
            
        return primaryIds.Concat(linkedIds).Distinct().ToList();
    }
    public async Task<bool> HasActiveSession(int tableId)
    {
        return await _context.DiningSessions
            .Include(ds => ds.DiningSessionTables)
            .AnyAsync(ds =>
                (ds.TableId == tableId || ds.DiningSessionTables.Any(dst => dst.TableId == tableId)) &&
                ds.Status == DiningSessionStatus.Active);
    }
    public override async Task<DiningSession?> Get(int sessionId)
    {
        return await _context.DiningSessions
            .Include(ds => ds.Table)
            .Include(ds => ds.DiningSessionTables)
            .ThenInclude(dst => dst.Table)
            .FirstOrDefaultAsync(ds => ds.Id == sessionId);
    }

    public async Task<DiningSessionTable> LinkTable(int sessionId, int tableId)
    {
        var existing = await _context.DiningSessionTables
            .FirstOrDefaultAsync(dst => dst.DiningSessionId == sessionId && dst.TableId == tableId);
        if (existing != null)
            return existing;

        var link = new DiningSessionTable
        {
            DiningSessionId = sessionId,
            TableId = tableId,
            LinkedAt = DateTime.Now
        };
        _context.DiningSessionTables.Add(link);
        await _context.SaveChangesAsync();
        return link;
    }

    public async Task UnlinkTable(int sessionId, int tableId)
    {
        var link = await _context.DiningSessionTables
            .FirstOrDefaultAsync(dst => dst.DiningSessionId == sessionId && dst.TableId == tableId);
        if (link != null)
        {
            _context.DiningSessionTables.Remove(link);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<ICollection<int>> GetLinkedTableIds(int sessionId)
    {
        return await _context.DiningSessionTables
            .Where(dst => dst.DiningSessionId == sessionId)
            .Select(dst => dst.TableId)
            .ToListAsync();
    }
    }
