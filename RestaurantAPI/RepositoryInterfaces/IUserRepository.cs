using RestaurantAPI.Models;

namespace RestaurantAPI.RepositoryInterfaces;

public interface IUserRepository:IRepository<int,User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByMobileHashAsync(string mobileHash);
    Task<User?> GetByRole(UserRole role);
    Task<User?> GetActiveWaiter(int waiterId);
    Task<int> GetKitchenStaffId();
    IQueryable<User> GetWaitersQuery();
}
