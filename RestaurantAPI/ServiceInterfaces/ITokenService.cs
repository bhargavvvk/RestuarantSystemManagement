using RestaurantAPI.Models.DTOs;

namespace RestaurantAPI.ServiceInterfaces;

public interface ITokenService
{
    string CreateCustomerToken(CustomerTokenRequest request);
    string CreateEmployeeToken(EmployeeTokenRequest request);
}
