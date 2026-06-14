using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;

namespace RestaurantAPI.Repositories;

public class RestaurantTableRepository:AbstractRepository<int,RestaurantTable>,IRestaurentTableRepository
{
    public RestaurantTableRepository(RestaurantContext context):base(context)
    {

    }
    public async Task<RestaurantTable?> GetByQrIdentifier(string qrIdentifier)
    {
        return await _context.RestaurantTables.FirstOrDefaultAsync(t=>t.QrIdentifier==qrIdentifier &&!t.IsDeleted);
    }
    public async Task<RestaurantTable?> GetByTableNumber(string tableNumber)
    {
        return await _context.RestaurantTables.FirstOrDefaultAsync(t=>t.TableNumber==tableNumber && !t.IsDeleted);
    }
    public async Task<IEnumerable<RestaurantTable>> GetAssignedTablesWithSessions(int waiterId)
    {
        return await _context.RestaurantTables.Include(t => t.DiningSessions).Where(t =>
                                                                            t.AssignedWaiterId == waiterId &&
                                                                            !t.IsDeleted)
                                                                            .ToListAsync();
    }
    public async Task<ICollection<RestaurantTable>> GetAllNonDeletedTables()
    {
        return await _context.RestaurantTables
            .Where(t => !t.IsDeleted)
            .OrderBy(t => t.TableNumber)
            .ToListAsync();
    }
    public async Task<RestaurantTable?> GetTableDetails(int tableId)
    {
        return await _context.RestaurantTables
            .Include(t => t.AssignedWaiter)
            .Include(t => t.DiningSessions)
            .FirstOrDefaultAsync(t =>
                t.Id == tableId &&
                !t.IsDeleted);
    }
    public async Task<bool>
    HasAssignedTables(
        int waiterId)
    {
        return await _context.RestaurantTables
            .AnyAsync(t =>
                !t.IsDeleted &&
                t.AssignedWaiterId ==
                waiterId);
    }
}
