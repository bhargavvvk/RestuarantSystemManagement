using RestaurantAPI.Models;

namespace RestaurantAPI.RepositoryInterfaces;

public interface IRestaurantConfigurationRepository : IRepository<int, RestaurantConfiguration>
{
    /// <summary>Returns the single configuration row, or null if not seeded yet.</summary>
    Task<RestaurantConfiguration?> GetConfiguration();
}
