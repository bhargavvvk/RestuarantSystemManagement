using RestaurantAPI.Models;

namespace RestaurantAPI.RepositoryInterfaces;

public interface IArchiveAuditLogRepository
{
    Task<IEnumerable<AuditLog>> GetLogsBetweenDates(DateTime fromDate, DateTime toDate);
    Task CreateRange(IEnumerable<AuditLog> logs);
    Task<int> SaveChangesAsync();
}
