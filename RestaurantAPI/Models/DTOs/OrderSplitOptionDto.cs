namespace RestaurantAPI.Models.DTOs;

public class OrderSplitOptionDto
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal FoodTotal { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal ServiceChargeAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public ICollection<OrderItemResponseDto> Items { get; set; } = new List<OrderItemResponseDto>();
}
