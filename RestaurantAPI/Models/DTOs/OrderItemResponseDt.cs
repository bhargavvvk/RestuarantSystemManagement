namespace RestaurantAPI.Models.DTOs;

public class OrderItemResponseDto
{
    public int OrderItemId  { get; set; }
    public string ItemName { get; set; }
        = string.Empty;

    public decimal ItemPrice { get; set; }

    public int Quantity { get; set; }

    public OrderItemStatus Status { get; set; }
}
