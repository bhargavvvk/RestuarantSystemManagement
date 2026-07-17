using RestaurantAPI.Models;

namespace RestaurantAPI.RepositoryInterfaces;

public interface IIngredientRepository : IRepository<int, Ingredient>
{
    Task<Ingredient?> GetByName(string name);
    Task<ICollection<Ingredient>> Search(string? query);
}
