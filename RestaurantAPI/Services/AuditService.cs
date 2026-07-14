using System.Text;
using System.Text.Json;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Services;

public class AuditService:IAuditService
{
    private readonly IAuditLogRepository _auditRepository;
    private readonly IArchiveAuditLogRepository _archiveAuditRepository;

    public AuditService(IAuditLogRepository auditRepository, IArchiveAuditLogRepository archiveAuditRepository)
    {
        _auditRepository = auditRepository;
        _archiveAuditRepository = archiveAuditRepository;
    }
    public async Task LogAsync(string entityName, string entityId, string entityIdentifier, AuditAction action,
        object? oldValues = null, object? newValues = null, string? remarks = null)
    {
        var auditLog = new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            EntityIdentifier = entityIdentifier,
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
    public async Task<byte[]> DownloadLogs(DateTime fromDate, DateTime toDate)
    {
        if (fromDate > toDate)
            throw new InvalidDataException("From date cannot be greater than To date.");

        var mainLogs = await _auditRepository.GetLogsBetweenDates(fromDate, toDate);
        var archiveLogs = await _archiveAuditRepository.GetLogsBetweenDates(fromDate, toDate);

        var logs = mainLogs
            .Concat(archiveLogs)
            .OrderByDescending(l => l.PerformedAt)
            .ToList();

        if (!logs.Any())
            throw new AuditLogsNotFoundException();

        return GenerateCsv(logs);
    }
    private byte[] GenerateCsv(IEnumerable<AuditLog> logs)
    {
        var csv = new StringBuilder();

       
        csv.AppendLine("Performed At,Entity Name,Entity Identifier,Action,Remarks,Old Values,New Values");

        foreach (var log in logs)
        {
            csv.AppendLine(
                $"{Escape(log.PerformedAt.ToString("yyyy-MM-dd HH:mm:ss"))}," +
                $"{Escape(log.EntityName)}," +
                $"{Escape(log.EntityIdentifier)}," +
                $"{Escape(log.Action.ToString())}," +
                $"{Escape(log.Remarks)}," +
                $"{Escape(log.OldValues)}," +
                $"{Escape(log.NewValues)}");
        }
        return Encoding.UTF8.GetBytes(csv.ToString());
    }
    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
