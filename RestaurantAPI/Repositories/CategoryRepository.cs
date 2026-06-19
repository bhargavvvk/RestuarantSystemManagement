using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;

namespace RestaurantAPI.Repositories;

public class CategoryRepository : AbstractRepository<int, Category>, ICategoryRepository
{
    public CategoryRepository(RestaurantContext context) : base(context)
    {
    }
    public override async Task<Category?> Get(int categoryId)
    {
        return await _context.Categories
        .Include(c => c.MenuItems)
        .FirstOrDefaultAsync(
            c => c.Id == categoryId && !c.IsDeleted);
    }
    public async Task<Category?> GetByName(
    string name)
    {
        return await _context.Categories
            .FirstOrDefaultAsync(
                c => c.Name.ToLower() ==
                    name.ToLower() && !c.IsDeleted);
    }
    public override async Task<ICollection<Category>>
    GetAll()
{
    return await _context.Categories
        .Where(c => !c.IsDeleted)
        .ToListAsync();
}
}
