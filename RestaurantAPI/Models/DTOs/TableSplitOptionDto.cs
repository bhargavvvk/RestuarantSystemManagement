namespace RestaurantAPI.Models.DTOs;

public class TableSplitItemDto
{
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal ItemPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class TableSplitOptionDto
{
    public int TableId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public decimal FoodSubtotal { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal ServiceChargeAmount { get; set; }
    public decimal TableTotal { get; set; }
    public ICollection<TableSplitItemDto> Items { get; set; } = new List<TableSplitItemDto>();
}
