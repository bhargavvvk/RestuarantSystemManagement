using RestaurantAPI.Models;

namespace RestaurantAPI.RepositoryInterfaces;

public interface ICustomerRepository:IRepository<int, Customer>
{
    Task<Customer?>GetByPhoneNumberHash(string phoneNumberHash);
}
