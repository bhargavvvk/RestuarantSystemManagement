using RestaurantAPI.Models.DTOs;

namespace RestaurantAPI.ServiceInterfaces;

public interface ITaxConfigurationService
{
    Task<TaxConfigurationResponseDto> GetTaxConfiguration();
    Task UpdateTaxConfiguration(UpdateTaxConfigurationDto request);
}
