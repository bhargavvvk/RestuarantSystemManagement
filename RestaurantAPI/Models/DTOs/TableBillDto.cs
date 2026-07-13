namespace RestaurantAPI.Models.DTOs;

public class TableBillDto
{
    public string BillNumber { get; set; } = string.Empty;
    public int TableId { get; set; }
    public string TableNumber { get; set; } = string.Empty;

    // This table's food subtotal
    public decimal MyTableFoodTotal { get; set; }
    public decimal CgstPercentage { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstPercentage { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal ServiceChargePercentage { get; set; }
    public decimal ServiceChargeAmount { get; set; }
    public decimal MyTableTotal { get; set; }

    // Full session totals (all tables)
    public decimal SessionGrandTotal { get; set; }

    // Flag for group order
    public bool IsGroupOrder { get; set; }
    public int LinkedTablesCount { get; set; }

    public int PaymentStatus { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string? CustomSplitsJson { get; set; }
}
