namespace RestaurantAPI.Models.DTOs;

public class KitchenOrderDto
{
    public int OrderId { get; set; }
    public string OrderNumber {get; set;}=string.Empty;
    public string TableNumber { get; set; } = string.Empty;

    public DateTime PlacedAt { get; set; }

    public ICollection<KitchenOrderItemDto> Items { get; set; } = [];
}
