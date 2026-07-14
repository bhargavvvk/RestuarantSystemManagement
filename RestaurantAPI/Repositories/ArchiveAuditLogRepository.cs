using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;

namespace RestaurantAPI.Repositories;

public class ArchiveAuditLogRepository:AbstractRepository<int,AuditLog,ArchiveContext>,IArchiveAuditLogRepository
{
    public ArchiveAuditLogRepository(ArchiveContext context)
        : base(context)
    {

    }
     public async Task CreateRange(IEnumerable<AuditLog> logs)
    {
        await _context.AuditLogs.AddRangeAsync(logs);
    }
    public async Task<IEnumerable<AuditLog>> GetLogsBetweenDates(DateTime fromDate, DateTime toDate)
    {
        var endDate = toDate.Date.AddDays(1);

        return await _context.AuditLogs
            .Where(log => log.PerformedAt >= fromDate.Date &&
                          log.PerformedAt < endDate)
            .OrderByDescending(log => log.PerformedAt)
            .ToListAsync();
    }
}
