using RestaurantAPI.Models;

namespace RestaurantAPI.RepositoryInterfaces;

public interface IDiningSessionRepository:IRepository<int,DiningSession>
{
    Task<DiningSession?> GetActiveSessionByTableId(int tableId);
    Task<DiningSession?> GetActiveSessionByOtp(string otp);
    Task<DiningSession?> GetActiveSessionWithCartByTableId(int tableId);
    Task<ICollection<int>> GetActiveTableIds();
    Task<bool> HasActiveSession(int tableId);
    Task<DiningSessionTable> LinkTable(int sessionId, int tableId);
    Task UnlinkTable(int sessionId, int tableId);
    Task<ICollection<int>> GetLinkedTableIds(int sessionId);
}
