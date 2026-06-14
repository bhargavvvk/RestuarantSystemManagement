namespace RestaurantAPI.Models.DTOs;

public class OrderRegistryDto
{
    public int OrderId { get; set; }
    public string OrderNumber {get; set;}=string.Empty;

    public string TableNumber { get; set; } = string.Empty;

    public DateTime PlacedAt { get; set; }

    public int ItemCount { get; set; }

    public string Status { get; set; } = string.Empty;
}
