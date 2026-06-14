using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;
using RestaurantAPI.RepositoryInterfaces;

namespace RestaurantAPI.Repositories;

public class AbstractRepository<K,T>: IRepository<K, T> where T : class
{
    protected  readonly RestaurantContext _context;
        public AbstractRepository(RestaurantContext context)
        {
            _context = context;
        }
        public async Task<T> Create(T item)
        {
            await _context.AddAsync(item);
            return item;
        }

        public virtual async Task<T?> Delete(K key)
        {
            throw new NotImplementedException("message");
        }

        public virtual async Task<T?> Get(K key)
        {
            var item = await _context.FindAsync<T>(key);
            return item;
        }

        public virtual async Task<ICollection<T>> GetAll()
        {
            return (await _context.Set<T>().ToListAsync());
        }

        public async Task<T?> Update(K key, T item)
        {
            var myItem = await Get(key);
            if (myItem == null)
                throw new Exception("No such item for update");
            _context.Set<T>().Update(item);
            return item;
        }
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
}
