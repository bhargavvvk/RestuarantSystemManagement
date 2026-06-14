

using System.ComponentModel.DataAnnotations;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Services;

public class TableService:ITableService
{
    private readonly IRestaurentTableRepository _restaurentTableRepository;
    private readonly ILogger<ITableService> _logger;
    private readonly IDiningSessionRepository _diningSessionRepository;
    private readonly IAuditService _auditService;
    private readonly IUserRepository _userRepository;
    public TableService(IRestaurentTableRepository restaurentTableRepository, ILogger<ITableService> logger, IDiningSessionRepository diningSessionRepository
    ,IAuditService auditService,IUserRepository userRepository)
    {
        _restaurentTableRepository = restaurentTableRepository;
        _logger = logger;
        _diningSessionRepository = diningSessionRepository;
        _auditService = auditService;
        _userRepository=userRepository;
    }

    public async Task<IEnumerable<WaiterTableResponseDto>> GetAssignedTablesAsync(int waiterId)
    {
        var tables =
        await _restaurentTableRepository.GetAssignedTablesWithSessions(waiterId);
        _logger.LogInformation("Getting the statuses of the table");
        var result = tables.Select(table =>
        {
            string status;

            if (table.Status == TableStatus.Unavailable)
            {
                status = "Unavailable";
            }
            else if (table.DiningSessions != null && table.DiningSessions.Any(ds => ds.Status == DiningSessionStatus.Active))
            {
                status = "Occupied";
            }
            else
            {
                status = "Available";
            }

            return new WaiterTableResponseDto
            {
                TableId = table.Id,
                TableNumber = table.TableNumber,
                Status = status
            };
        });

        return result;
    }
    public async Task<TableStatusResponseDto> GetTableStatus(string qrIdentifier)
    {
        var table = await _restaurentTableRepository.GetByQrIdentifier(qrIdentifier);
        _logger.LogInformation("Checking table status for QR Identifier: {QrIdentifier}",qrIdentifier);
        if(table == null)
        {
            _logger.LogInformation("Checking table status for QR Identifier: {QrIdentifier}",qrIdentifier);
            throw new TableNotFoundException();
        }
        if(table.Status == TableStatus.Unavailable)
        {
            _logger.LogInformation( "Table {TableNumber} is unavailable",table.TableNumber);
            return new TableStatusResponseDto
            {
                TableNumber = table.TableNumber,
                IsAvailable = false,
                HasActiveSession = false
            };
        }
        var activeSession =await _diningSessionRepository.GetActiveSessionByTableId(table.Id);
        _logger.LogInformation("Table {TableNumber} found. Active Session Exists: {HasActiveSession}",table.TableNumber,activeSession != null);
        return new TableStatusResponseDto
        {
            TableNumber = table.TableNumber,
            IsAvailable = true,
            HasActiveSession = activeSession != null
        };
    }
    public async Task<TableDashboardResponseDto> GetTableDashboard()
    {
        var tables = await _restaurentTableRepository.GetAllNonDeletedTables();

        var occupiedTableIds =(await _diningSessionRepository.GetActiveTableIds()).ToHashSet();
        var tableDtos = new List<TableDashboardDto>();
        int availableCount = 0;
        int occupiedCount = 0;
        int unavailableCount = 0;

        foreach (var table in tables)
        {
            string status;

            if (occupiedTableIds.Contains(table.Id))
            {
                status = "Occupied";
                occupiedCount++;
            }
            else if (table.Status == TableStatus.Unavailable)
            {
                status = "Unavailable";
                unavailableCount++;
            }
            else
            {
                status = "Available";
                availableCount++;
            }

            tableDtos.Add(new TableDashboardDto
            {
                Id = table.Id,
                TableNumber = table.TableNumber,
                Status = status
            });
        }

        return new TableDashboardResponseDto
        {
            Summary = new TableDashboardCountsDto
            {
                TotalTables = tables.Count,
                AvailableTables = availableCount,
                OccupiedTables = occupiedCount,
                UnavailableTables = unavailableCount
            },
            Tables = tableDtos
        };
    }
    public async Task<RestaurantTable> UpdateTableAvailability(int tableId,TableStatus status)
    {
        var table = await _restaurentTableRepository.Get(tableId);

        if (table == null || table.IsDeleted)   throw new TableNotFoundException();

        var hasActiveSession =await _diningSessionRepository.HasActiveSession(tableId);

        if (hasActiveSession)
            throw new Exception("Cannot update availability during active dining session");

        if (table.Status == status)
            throw new Exception("Table already has the requested status");

        var oldValues = new
        {
            table.Status
        };

        table.Status = status;

        await _auditService.LogAsync(
            nameof(RestaurantTable),
            table.Id.ToString(),
            AuditAction.Updated,
            oldValues,
            new
            {
                table.Status
            },
            "Table availability updated");

        await _restaurentTableRepository.SaveChangesAsync();

        return table;
    }
    public async Task DeleteTable(int tableId)
    {
        var table = await _restaurentTableRepository.Get(tableId);

        if (table == null || table.IsDeleted)
            throw new TableNotFoundException();
        var hasActiveSession =await _diningSessionRepository.HasActiveSession(tableId);
        if (hasActiveSession)
            throw new Exception("Cannot delete table with active dining session");

        table.IsDeleted = true;
        table.Status = TableStatus.Unavailable;
        table.AssignedWaiter=null;

        await _auditService.LogAsync(
            nameof(RestaurantTable),
            table.Id.ToString(),
            AuditAction.Deleted,
            new
            {
                table.TableNumber,
                table.Status
            },
            null,
            $"Table {table.TableNumber} soft deleted");

        await _restaurentTableRepository.SaveChangesAsync();
    }
    public async Task<TableResponseDto> AddTable(AddTableRequestDto request)
    {
        if (request.Capacity <= 0)
        {
            throw new ValidationException("Capacity must be greater than zero");
        }
        var existingTable =await _restaurentTableRepository.GetByTableNumber(request.TableNumber);
        if (existingTable != null)
        {
            throw new ValidationException("Table number already exists");
        }
        if (request.AssignedWaiterId.HasValue)
        {
            var waiter =await _userRepository.GetActiveWaiter(request.AssignedWaiterId.Value);
            if (waiter == null)
            {
                throw new WaiterNotFoundException("Assigned waiter not found");
            }
        }
        var table = new RestaurantTable
        {
            TableNumber = request.TableNumber,
            Capacity = request.Capacity,
            AssignedWaiterId = request.AssignedWaiterId,
            QrIdentifier = $"TBL_{Guid.NewGuid():N}",
            Status = request.AssignedWaiterId.HasValue
                ? TableStatus.Available
                : TableStatus.Unavailable
        };

        await _restaurentTableRepository.Create(table);

        await _restaurentTableRepository.SaveChangesAsync();

        await _auditService.LogAsync(
            nameof(RestaurantTable),
            table.Id.ToString(),
            AuditAction.Created,
            null,
            new
            {
                table.TableNumber,
                table.Capacity,
                table.AssignedWaiterId,
                table.Status
            },
            "Restaurant table created");

        await _restaurentTableRepository.SaveChangesAsync();

        return new TableResponseDto
        {
            Id = table.Id,
            TableNumber = table.TableNumber,
            Capacity = table.Capacity,
            QrIdentifier = table.QrIdentifier,
            Status = table.Status,
            AssignedWaiterId = table.AssignedWaiterId
        };
    }
    public async Task<TableDetailsDto> GetTableDetails(int tableId)
    {
        var table =await _restaurentTableRepository.GetTableDetails(tableId);
        if (table == null)
            throw new TableNotFoundException();

        var activeSession =
            table.DiningSessions?
                .FirstOrDefault(ds =>
                    ds.Status ==
                    DiningSessionStatus.Active);
        if (activeSession == null)
                throw new SessionNotFoundException();

        return new TableDetailsDto
        {
            TableId = table.Id,
            TableNumber = table.TableNumber,
            Status = "Occupied",
            AssignedWaiterId = table.AssignedWaiterId,
            AssignedWaiterName = table.AssignedWaiter?.Name,
            SessionStartedAt = activeSession.StartedAt
        };
    }
}
