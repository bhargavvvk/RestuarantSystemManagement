using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;

namespace RestaurantAPI.ServiceInterfaces;

public interface ITableService
{
    Task<IEnumerable<WaiterTableResponseDto>> GetAssignedTablesAsync(int waiterId);
    Task<TableStatusResponseDto> GetTableStatus(string qrIdentifier);
    Task<TableDashboardResponseDto> GetTableDashboard();
    Task<RestaurantTable> UpdateTableAvailability(int tableId, TableStatus status);
    Task DeleteTable(int tableId);
    Task<TableResponseDto> AddTable(AddTableRequestDto request);
    Task<TableDetailsDto> GetTableDetails(int tableId);
}
