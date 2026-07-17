using RestaurantAPI.Models;

namespace RestaurantAPI.RepositoryInterfaces;

public interface IMenuItemIngredientRepository : IRepository<int, MenuItemIngredient>
{
    Task<ICollection<MenuItemIngredient>> GetByMenuItemId(int menuItemId);
    Task DeleteAllForMenuItem(int menuItemId);
}
