namespace RestaurantAPI.Models.DTOs;

public class CartItemResponseDto
{

    public int Id { get; set; }

    public int MenuItemId { get; set; }

    public string MenuItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
}
