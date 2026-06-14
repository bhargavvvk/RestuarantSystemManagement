using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;

namespace RestaurantAPI.Repositories;

public class TaxConfigurationRepository : AbstractRepository<int, TaxConfiguration>, ITaxConfigurationRepository
{
    public TaxConfigurationRepository(RestaurantContext context) : base(context)
    {
    }

    public async Task<TaxConfiguration?> GetActiveConfiguration()
    {
        return await _context.TaxConfigurations.FirstOrDefaultAsync(tc =>tc.IsActive);
    }
}
