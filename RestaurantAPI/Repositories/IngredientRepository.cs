using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;

namespace RestaurantAPI.Repositories;

public class IngredientRepository : AbstractRepository<int, Ingredient, RestaurantContext>, IIngredientRepository
{
    public IngredientRepository(RestaurantContext context) : base(context) { }

    public override async Task<ICollection<Ingredient>> GetAll()
    {
        return await _context.Ingredients
            .Where(i => !i.IsDeleted)
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    public override async Task<Ingredient?> Get(int id)
    {
        return await _context.Ingredients
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
    }

    public async Task<Ingredient?> GetByName(string name)
    {
        var normalized = name.Trim().ToUpper();
        return await _context.Ingredients
            .FirstOrDefaultAsync(i => !i.IsDeleted && i.Name.Trim().ToUpper() == normalized);
    }

    public async Task<ICollection<Ingredient>> Search(string? query)
    {
        var q = _context.Ingredients.Where(i => !i.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim().ToUpper();
            q = q.Where(i => i.Name.ToUpper().Contains(normalized));
        }

        return await q.OrderBy(i => i.Name).ToListAsync();
    }
}
