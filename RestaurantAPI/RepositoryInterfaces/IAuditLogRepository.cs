using RestaurantAPI.Models;

namespace RestaurantAPI.RepositoryInterfaces;

public interface IAuditLogRepository: IRepository<int, AuditLog>
{
    Task<IEnumerable<AuditLog>> GetLogsBetweenDates(DateTime fromDate, DateTime toDate);
    Task<IEnumerable<AuditLog>> GetLogsOlderThan(DateTime cutoffDate);
    Task DeleteRange(IEnumerable<AuditLog> logs);
}
