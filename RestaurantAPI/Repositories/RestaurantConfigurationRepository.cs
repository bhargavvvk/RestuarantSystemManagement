using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;

namespace RestaurantAPI.Repositories;

public class RestaurantConfigurationRepository
    : AbstractRepository<int, RestaurantConfiguration, RestaurantContext>,
      IRestaurantConfigurationRepository
{
    public RestaurantConfigurationRepository(RestaurantContext context) : base(context) { }

    public async Task<RestaurantConfiguration?> GetConfiguration()
    {
        return await _context.RestaurantConfigurations.FirstOrDefaultAsync();
    }
}
