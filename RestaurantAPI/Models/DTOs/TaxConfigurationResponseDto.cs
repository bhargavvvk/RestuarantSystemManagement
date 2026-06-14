namespace RestaurantAPI.Models.DTOs;

public class TaxConfigurationResponseDto
{
    public decimal CgstPercentage { get; set; }

    public decimal SgstPercentage { get; set; }

    public decimal ServiceChargePercentage { get; set; }
}