using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;

namespace RestaurantAPI.Repositories;

public class CustomerRepository:AbstractRepository<int,Customer>,ICustomerRepository
{
    public CustomerRepository(RestaurantContext context) : base(context)
    {

    }
    public Task<Customer?> GetByPhoneNumberHash(string phoneNumberHash)
    {
        return _context.Customers.FirstOrDefaultAsync(c => c.MobileNumberHash == phoneNumberHash);
    }
}
