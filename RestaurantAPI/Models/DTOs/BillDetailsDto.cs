namespace RestaurantAPI.Models.DTOs;

public class BillDetailsDto
{
    public string BillNumber { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; }

    public string TableNumber { get; set; } = string.Empty;

    public int WaiterId { get; set; }

    public string WaiterName { get; set; } = string.Empty;

    public PaymentMethod? PaymentMethod { get; set; }

    public PaymentStatus PaymentStatus { get; set; }

    public decimal FoodTotal { get; set; }

    public decimal CgstPercentage { get; set; }

    public decimal CgstAmount { get; set; }

    public decimal SgstPercentage { get; set; }

    public decimal SgstAmount { get; set; }

    public decimal ServiceChargePercentage { get; set; }

    public decimal ServiceChargeAmount { get; set; }

    public decimal GrandTotal { get; set; }
}
