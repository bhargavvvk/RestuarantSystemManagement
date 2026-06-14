namespace RestaurantAPI.Models.DTOs;

public class OrderResponseDto
{
    public int OrderId { get; set; }
    public string OrderNumber {get;set;}=string.Empty;
    public decimal TotalAmount { get; set; }

    public DateTime PlacedAt { get; set; }
    public string? SpecialInstructions { get; set; }
    public ICollection<OrderItemResponseDto>
        Items { get; set; } = [];
}
