namespace RestaurantAPI.Models.DTOs;

public class KitchenOrdersResponseDto
{
    public int QueueCount { get; set; }

    public int PreparingCount { get; set; }

    public int ReadyCount { get; set; }

    public ICollection<KitchenOrderDto> Orders { get; set; } = [];
}
