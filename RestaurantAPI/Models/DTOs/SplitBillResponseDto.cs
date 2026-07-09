namespace RestaurantAPI.Models.DTOs;

public class SplitBillResponseDto
{
    public decimal FoodTotal { get; set; }
    public decimal CgstPercentage { get; set; }
    public decimal SgstPercentage { get; set; }
    public decimal ServiceChargePercentage { get; set; }
    public decimal GrandTotal { get; set; }

    public ICollection<OrderSplitOptionDto> OrderSplits { get; set; } = new List<OrderSplitOptionDto>();
    public ICollection<ItemSplitOptionDto> ItemSplits { get; set; } = new List<ItemSplitOptionDto>();
    public string? CustomSplitsJson { get; set; }
}
