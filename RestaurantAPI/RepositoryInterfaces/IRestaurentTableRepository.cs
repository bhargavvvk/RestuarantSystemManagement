using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;

namespace RestaurantAPI.RepositoryInterfaces;

public interface IRestaurentTableRepository:IRepository<int, RestaurantTable>
{
    Task<RestaurantTable?> GetByQrIdentifier(string qrIdentifier);
    Task<RestaurantTable?> GetByTableNumber(string tableNumber);
    Task<IEnumerable<RestaurantTable>> GetAssignedTablesWithSessions(int waiterId);
    Task<ICollection<RestaurantTable>> GetAllNonDeletedTables();
    Task<RestaurantTable?> GetTableDetails(int tableId);
    Task<bool> HasAssignedTables(int waiterId);
}
