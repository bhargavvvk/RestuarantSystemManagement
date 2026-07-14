using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;

namespace RestaurantAPI.Repositories;

public class AuditLogRepository:AbstractRepository<int,AuditLog,RestaurantContext>,IAuditLogRepository
{
    public AuditLogRepository(RestaurantContext context)
        : base(context)
    {

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
     public async Task DeleteRange(IEnumerable<AuditLog> logs)
    {
        _context.AuditLogs.RemoveRange(logs);
        await Task.CompletedTask;
    }
    public async Task<IEnumerable<AuditLog>> GetLogsOlderThan(DateTime cutoffDate)
    {
        return await _context.AuditLogs
            .Where(log => log.PerformedAt < cutoffDate)
            .OrderBy(log => log.PerformedAt)
            .ToListAsync();
    }
}
