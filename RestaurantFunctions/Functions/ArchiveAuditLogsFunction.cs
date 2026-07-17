using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantFunctions.Functions;

public class ArchiveAuditLogsFunction
{
    private readonly IAuditArchiveService _archiveService;
    private readonly ILogger<ArchiveAuditLogsFunction> _logger;

    public ArchiveAuditLogsFunction(
        IAuditArchiveService archiveService,
        ILogger<ArchiveAuditLogsFunction> logger)
    {
        _archiveService = archiveService;
        _logger = logger;
    }

    [Function("ArchiveAuditLogs")]
    public async Task Run(
        [TimerTrigger("0 */2 * * * *")] TimerInfo timer)
    {
        _logger.LogInformation("Archive Audit Logs function started at {Time}",
            DateTime.UtcNow);

        await _archiveService.ArchiveOldLogs();

        _logger.LogInformation("Archive Audit Logs function completed at {Time}",
            DateTime.UtcNow);
    }
}