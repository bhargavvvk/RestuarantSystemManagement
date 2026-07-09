namespace RestaurantAPI.Models.DTOs;

public class ItemSplitOptionDto
{
    public int OrderItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal ItemPrice { get; set; }
    public decimal FoodTotal { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal ServiceChargeAmount { get; set; }
    public decimal GrandTotal { get; set; }
}
