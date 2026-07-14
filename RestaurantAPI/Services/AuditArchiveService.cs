using Microsoft.Extensions.Configuration;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Services;

public class AuditArchiveService : IAuditArchiveService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IArchiveAuditLogRepository _archiveAuditLogRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuditArchiveService> _logger;

    public AuditArchiveService(
        IAuditLogRepository auditLogRepository,
        IArchiveAuditLogRepository archiveAuditLogRepository,
        IConfiguration configuration,
        ILogger<AuditArchiveService> logger)
    {
        _auditLogRepository = auditLogRepository;
        _archiveAuditLogRepository = archiveAuditLogRepository;
        _configuration = configuration;
        _logger = logger;
    }
    public async Task ArchiveOldLogs()
    {
        var retentionDays = _configuration.GetValue<int>("AuditLogArchive:RetentionInDays");
        var cutoffDate = DateTime.Now.AddDays(-retentionDays);

        var logs = (await _auditLogRepository.GetLogsOlderThan(cutoffDate)).ToList();

        if (!logs.Any())
        {
            _logger.LogInformation("No audit logs found for archival.");
            return;
        }

        try
        {
            await _archiveAuditLogRepository.CreateRange(logs);
            await _archiveAuditLogRepository.SaveChangesAsync();

            await _auditLogRepository.DeleteRange(logs);
            await _auditLogRepository.SaveChangesAsync();

            _logger.LogInformation(
                "{Count} audit logs archived successfully.",
                logs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to archive audit logs.");

            throw;
        }
    }
}
