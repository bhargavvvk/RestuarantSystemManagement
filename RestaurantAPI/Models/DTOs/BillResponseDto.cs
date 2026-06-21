namespace RestaurantAPI.Models.DTOs;

public class BillResponseDto
{
    public string BillNumber { get; set; }= string.Empty;

    public decimal FoodTotal { get; set; }

    public decimal CgstPercentage { get; set; }

    public decimal CgstAmount { get; set; }

    public decimal SgstPercentage { get; set; }

    public decimal SgstAmount { get; set; }

    public decimal ServiceChargePercentage { get; set; }

    public decimal ServiceChargeAmount { get; set; }

    public decimal GrandTotal { get; set; }

    public PaymentStatus PaymentStatus { get; set; }

    public DateTime GeneratedAt { get; set; }
}
