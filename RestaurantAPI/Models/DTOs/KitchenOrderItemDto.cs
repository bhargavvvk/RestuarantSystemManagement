namespace RestaurantAPI.Models.DTOs;

public class KitchenOrderItemDto
{
    public int OrderItemId { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public OrderItemStatus Status { get; set; }
}
