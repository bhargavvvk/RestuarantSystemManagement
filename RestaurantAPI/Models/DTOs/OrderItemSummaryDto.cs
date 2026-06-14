namespace RestaurantAPI.Models.DTOs;

public class OrderItemSummaryDto
{
    public string ItemName { get; set; } = string.Empty;

    public int Quantity { get; set; }
}