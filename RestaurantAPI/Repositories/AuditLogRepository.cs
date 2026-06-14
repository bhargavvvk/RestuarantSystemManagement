using RestaurantAPI.Contexts;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;

namespace RestaurantAPI.Repositories;

public class AuditLogRepository:AbstractRepository<int,AuditLog>,IAuditLogRepository
{
    public AuditLogRepository(RestaurantContext context)
        : base(context)
    {
    }
}
