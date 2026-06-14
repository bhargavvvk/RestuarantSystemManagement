namespace RestaurantAPI.RepositoryInterfaces;

 public interface IRepository<K,T> where T : class
    {
        public Task<T> Create(T item);
        public Task<T?> Get(K key);
        public Task<ICollection<T>> GetAll();
        public Task<T?> Update(K key,T item);
        public Task<T?> Delete(K key);
        public Task<int> SaveChangesAsync();
}
