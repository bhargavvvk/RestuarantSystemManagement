using RestaurantAPI.Models;

namespace RestaurantAPI.ServiceInterfaces;

public interface IAuditService
{
    Task LogAsync(string entityName, string entityId, string entityIdentifier, AuditAction action,
        object? oldValues = null, object? newValues = null, string? remarks = null);
    Task<byte[]> DownloadLogs(DateTime fromDate, DateTime toDate);
}
