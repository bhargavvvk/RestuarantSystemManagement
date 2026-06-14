using RestaurantAPI.Models;

namespace RestaurantAPI.RepositoryInterfaces;

public interface ICategoryRepository:IRepository<int,Category>
{
   Task<Category?> GetByName(string name);
}
