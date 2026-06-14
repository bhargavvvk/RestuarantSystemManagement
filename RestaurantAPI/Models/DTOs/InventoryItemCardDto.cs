namespace RestaurantAPI.Models.DTOs;

public class InventoryItemCardDto
{
    public int Id { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public decimal CurrentQuantity { get; set; }

    public decimal ThresholdQuantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }
}
