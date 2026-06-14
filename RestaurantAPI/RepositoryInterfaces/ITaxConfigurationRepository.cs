using RestaurantAPI.Models;

namespace RestaurantAPI.RepositoryInterfaces;

public interface ITaxConfigurationRepository:IRepository<int,TaxConfiguration>
{
    Task<TaxConfiguration?> GetActiveConfiguration();
}
