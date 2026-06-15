using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Services;

public class AdminService : IAdminService
{
    private readonly IRestaurentTableRepository _restaurentTableRepository;
    private readonly IIOrderService _orderService;
    private readonly IBillService _billService;
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDiningSessionRepository _diningSessionRepository;
    private readonly IAuditService _auditService;
    private readonly RestaurantContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<AdminService> _logger;
    public AdminService(
        IRestaurentTableRepository restaurentTableRepository,
        IIOrderService orderService,
        IBillService billService,
        IOrderRepository orderRepository,
        IUserRepository userRepository,
        IDiningSessionRepository diningSessionRepository,
        IAuditService auditService,
        RestaurantContext context,
        IHubContext<NotificationHub> hubContext,
        ILogger<AdminService> logger)
    {
        _restaurentTableRepository = restaurentTableRepository;
        _orderService = orderService;
        _billService = billService;
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _diningSessionRepository = diningSessionRepository;
        _auditService = auditService;
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }
    public async Task<ICollection<OrderResponseDto>>GetTableOrders(int tableId)
    {
        _logger.LogInformation("Getting orders for table {TableId}", tableId);
        var session = await GetActiveSessionForTable(tableId);
        return await _orderService.GetOrders(session.Id);
    }
    public async Task<BillResponseDto>GetTableBill(int tableId)
    {
        _logger.LogInformation("Getting bill for table {TableId}", tableId);
        var session = await GetActiveSessionForTable(tableId);
        return await _billService.GetBill(session.Id);
    }

    public async Task<BillResponseDto> UpdateServiceCharge(int tableId,bool includeServiceCharge)
    {
        _logger.LogInformation("Updating service charge for table {TableId} to {IncludeServiceCharge}", tableId, includeServiceCharge);
        var session = await GetActiveSessionForTable(tableId);
        return await _billService.UpdateServiceCharge(session.Id,includeServiceCharge);
    }

    private async Task<DiningSession>GetActiveSessionForTable(int tableId)
    {
        var table =await _restaurentTableRepository.GetTableDetails(tableId);
        if (table == null) throw new TableNotFoundException();
        var activeSession =table.DiningSessions?.FirstOrDefault(ds =>ds.Status == DiningSessionStatus.Active);
        if (activeSession == null)throw new SessionNotFoundException();
        return activeSession;
    }

    public async Task CancelOrder(int tableId,int orderId)
    {
        _logger.LogInformation("Admin cancelling order {OrderId} for table {TableId}", orderId, tableId);
        var session =await GetActiveSessionForTable(tableId);
        var order =await _orderRepository.Get(orderId);
        if (order == null)
        {
            throw new OrderNotFoundException();
        }
        if (order.DiningSessionId != session.Id)
        {
            throw new Exception("Order does not belong to the specified table");
        }
        await _orderService.CancelOrder(orderId);
        _logger.LogInformation("Order {OrderId} cancelled by admin for table {TableId}", orderId, tableId);
    }

    public async Task CancelOrderItem(int tableId,int orderId,int orderItemId)
    {
        _logger.LogInformation("Admin cancelling order item {OrderItemId} from order {OrderId} for table {TableId}", orderItemId, orderId, tableId);
        var session =await GetActiveSessionForTable(tableId);
        var order =await _orderRepository.Get(orderId);
        if (order == null)
        {
            throw new OrderNotFoundException();
        }
        if (order.DiningSessionId != session.Id)
        {
            throw new Exception("Order does not belong to the specified table");
        }
        await _orderService.CancelOrderItem(orderId,orderItemId);
        _logger.LogInformation("Order item {OrderItemId} cancelled by admin", orderItemId);
    }

    public async Task UpdateOrderItemQuantity(int tableId,int orderId,int orderItemId,int quantity)
    {
        _logger.LogInformation("Admin updating quantity of order item {OrderItemId} to {Quantity} for table {TableId}", orderItemId, quantity, tableId);
        var session =await GetActiveSessionForTable(tableId);
        var order =await _orderRepository.Get(orderId);
        if (order == null)
        {
            throw new OrderNotFoundException();
        }
        if (order.DiningSessionId != session.Id)
        {
            throw new Exception("Order does not belong to the specified table");
        }
        await _orderService.UpdateOrderItemQuantity(orderId,orderItemId,quantity);
        _logger.LogInformation("Order item {OrderItemId} quantity updated to {Quantity} by admin", orderItemId, quantity);
    }

    public async Task<WaiterManagementResponseDto> GetWaiters(string? search,bool? isActive)
    {
        _logger.LogInformation("Getting waiters list (search={Search}, isActive={IsActive})", search, isActive);
        var query =_userRepository.GetWaitersQuery();
        var summary = new WaiterSummaryDto
        {
            TotalWaiters =await query.CountAsync(),
            ActiveWaiters =await query.CountAsync(w => w.IsActive),
            InactiveWaiters =await query.CountAsync(w => !w.IsActive)
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTrimmed = search.Trim().ToLower();
            query = query.Where(w =>w.Name.ToLower().Contains(searchTrimmed));
        }

        if (isActive.HasValue)
        {
            query = query.Where(w =>w.IsActive ==isActive.Value);
        }

        var waiters =await query.ToListAsync();
        var tables =await _restaurentTableRepository.GetAll();
        var waiterCards =waiters.Select(waiter =>
        {
            var assignedTables =tables.Where(t =>!t.IsDeleted &&t.AssignedWaiterId ==waiter.Id)
                                                    .Select(t =>t.TableNumber).ToList();

            return new WaiterCardDto
            {
                WaiterId = waiter.Id,
                Name = waiter.Name,
                IsActive =waiter.IsActive,
                AssignedTableCount =assignedTables.Count,
                AssignedTables =assignedTables
            };
        }).ToList();

        return new WaiterManagementResponseDto
        {
            Summary = summary,
            Waiters = waiterCards
        };
    }

    public async Task<TableResponseDto> AssignWaiter(int tableId,int waiterId)
    {
        _logger.LogInformation("Assigning waiter {WaiterId} to table {TableId}", waiterId, tableId);
        var table = await _restaurentTableRepository.Get(tableId);
        if (table == null || table.IsDeleted)throw new TableNotFoundException();
        if (await _diningSessionRepository.HasActiveSession(tableId))   throw new Exception("Cannot change waiter assignment during active dining session");
        var waiter = await _userRepository.GetActiveWaiter(waiterId);
        if (waiter == null) throw new WaiterNotFoundException("Waiter not found");
        var oldValues = new
        {
            table.AssignedWaiterId,
            table.Status
        };
        table.AssignedWaiterId = waiterId;
        await _auditService.LogAsync(
            nameof(RestaurantTable),
            table.Id.ToString(),
            AuditAction.Updated,
            oldValues,
            new { table.AssignedWaiterId, table.Status },
            "Waiter assigned to table");

        await _restaurentTableRepository.SaveChangesAsync();
       var tableNumber=table.TableNumber;
        await _hubContext.Clients.User(waiterId.ToString()).SendAsync("tableassinged", $"{tableNumber} is assinged to you");
        await _hubContext.Clients.User(waiter.Id.ToString()).SendAsync("tableremoved",$"{tableNumber} is reassinged to other");
        _logger.LogInformation("Waiter {WaiterId} assigned to table {TableId}", waiterId, tableId);
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
    public async Task<TableResponseDto> RemoveWaiter(int tableId)
    {
        _logger.LogInformation("Removing waiter from table {TableId}", tableId);
        var table = await _restaurentTableRepository.Get(tableId);
        if (table == null || table.IsDeleted)throw new TableNotFoundException();
        if (await _diningSessionRepository.HasActiveSession(tableId))
                throw new Exception("Cannot change waiter assignment during active dining session");
        var oldValues = new
        {
            table.AssignedWaiterId,
            table.Status
        };
        table.AssignedWaiterId = null;
        table.Status = TableStatus.Unavailable;

        await _auditService.LogAsync(
            nameof(RestaurantTable),
            table.Id.ToString(),
            AuditAction.Updated,
            oldValues,
            new { table.AssignedWaiterId, table.Status },
            "Waiter removed from table");

        await _restaurentTableRepository.SaveChangesAsync();
        await _hubContext.Clients.User(table.AssignedWaiterId.ToString()!).SendAsync("tableremoved",$"{table.TableNumber} is removed");
        _logger.LogInformation("Waiter removed from table {TableId}", tableId);
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
    public async Task UpdateWaiterStatus(int waiterId, bool isActive)
    {
        _logger.LogInformation("Updating waiter {WaiterId} status to {IsActive}", waiterId, isActive);
        var waiter = await _userRepository.Get(waiterId);

        if (waiter == null || waiter.Role != UserRole.Waiter)
            throw new WaiterNotFoundException("Waiter not found");

        if (!isActive)
        {
            var hasAssignedTables = await _restaurentTableRepository.HasAssignedTables(waiterId);
            if (hasAssignedTables)
                throw new Exception("Cannot deactivate waiter while tables are assigned");
        }

        var oldValues = new { waiter.IsActive };
        waiter.IsActive = isActive;

        await _auditService.LogAsync(
            nameof(User),
            waiter.Id.ToString(),
            AuditAction.Updated,
            oldValues,
            new { waiter.IsActive },
            isActive ? "Waiter activated" : "Waiter deactivated");
        await _hubContext.Clients.User(waiterId.ToString()).SendAsync("statuschange", "your account status as changed");
        await _userRepository.SaveChangesAsync();
        _logger.LogInformation("Waiter {WaiterId} status updated to {IsActive}", waiterId, isActive);
    }
    public async Task DeleteWaiter(int waiterId)
    {
        _logger.LogInformation("Deleting waiter {WaiterId}", waiterId);
        var waiter = await _userRepository.Get(waiterId);

        if (waiter == null || waiter.Role != UserRole.Waiter) throw new WaiterNotFoundException("Waiter not found");

        var hasAssignedTables = await _restaurentTableRepository.HasAssignedTables(waiterId);
        if (hasAssignedTables)
            throw new Exception("Cannot delete waiter while tables are assigned");

        var oldValues = new { waiter.IsDeleted, waiter.IsActive };

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            waiter.IsDeleted = true;
            waiter.IsActive = false;

            await _userRepository.SaveChangesAsync();

            await _auditService.LogAsync(
                nameof(User),
                waiter.Id.ToString(),
                AuditAction.Deleted,
                oldValues,
                new { waiter.IsDeleted },
                $"Waiter {waiter.Name} deleted");

            await _userRepository.SaveChangesAsync();
            await transaction.CommitAsync();
            await _hubContext.Clients.User(waiterId.ToString()).SendAsync("accountdeleted", "your account is deleted contact admin for recovery");
            _logger.LogInformation("Waiter {WaiterId} deleted", waiterId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
