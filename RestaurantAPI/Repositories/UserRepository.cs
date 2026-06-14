using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;

namespace RestaurantAPI.Repositories;

public class UserRepository:AbstractRepository<int,User>,IUserRepository
{
    public UserRepository(RestaurantContext context):base(context)
    {

    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
       var normalizedUsername =username.Trim().ToUpper();

    return await _context.Users.SingleOrDefaultAsync(u =>!u.IsDeleted &&u.Username.ToUpper() ==normalizedUsername);
    }
    public async Task<User?> GetByMobileHashAsync(string mobileHash)
    {
        return await _context.Users.SingleOrDefaultAsync(u => u.MobileNumberHash == mobileHash && !u.IsDeleted);
    }
    public async Task<User?> GetByRole(UserRole role)
    {
        return await _context.Users.FirstOrDefaultAsync(u =>u.Role == role &&!u.IsDeleted);
    }
    public async Task<User?> GetActiveWaiter(int waiterId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Id == waiterId &&
                u.Role == UserRole.Waiter &&
                u.IsActive &&
                !u.IsDeleted);
    }

    public async Task<int> GetKitchenStaffId()
    {
        return await _context.Users
            .Where(u => u.Role == UserRole.KitchenStaff && u.IsActive && !u.IsDeleted)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();
    }
    public IQueryable<User> GetWaitersQuery()
    {
        return _context.Users
            .Where(u =>
                u.Role == UserRole.Waiter &&
                !u.IsDeleted)
            .AsQueryable();
    }
    public override async Task<User?> Get(int userId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Id == userId &&
                !u.IsDeleted);
    }
}
