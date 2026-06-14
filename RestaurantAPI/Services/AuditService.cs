using System.Text.Json;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Services;

public class AuditService:IAuditService
{
    private readonly IAuditLogRepository _auditRepository;

    public AuditService(IAuditLogRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }
    public async Task LogAsync(string entityName,string entityId,AuditAction action,
        object? oldValues = null,object? newValues = null,string? remarks = null)
    {
        var auditLog = new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            OldValues = oldValues == null
                ? null
                : JsonSerializer.Serialize(oldValues),

            NewValues = newValues == null
                ? null
                : JsonSerializer.Serialize(newValues),

            Remarks = remarks,
            PerformedAt = DateTime.Now
        };

        await _auditRepository.Create(auditLog);
    }
}
