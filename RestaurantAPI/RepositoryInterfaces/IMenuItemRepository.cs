using RestaurantAPI.Models;

namespace RestaurantAPI.RepositoryInterfaces;

public interface IMenuItemRepository:IRepository<int, MenuItem>
{
    Task<MenuItem?> GetByName(string name);
    IQueryable<MenuItem> GetMenuQuery();
}
