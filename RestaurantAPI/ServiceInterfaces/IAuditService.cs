using RestaurantAPI.Models;

namespace RestaurantAPI.ServiceInterfaces;

public interface IAuditService
{
    Task LogAsync(string entityName, string entityId, AuditAction action,
        object? oldValues = null,object? newValues = null,string? remarks = null);
}
