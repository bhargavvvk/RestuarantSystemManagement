using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.Models;

public enum PaymentStatus
{
    Pending,
    Paid
}

public enum PaymentMethod
{
    Cash,
    UPI,
    Card,
    District,
    Swiggy
}

public class Bill
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Bill number is required.")]
    public string BillNumber {get; set;}=string.Empty;
    [Required(ErrorMessage = "Dining session is required.")]
    public int DiningSessionId { get; set; }

    public DiningSession? DiningSession { get; set; }

    [Required]
    public int TaxConfigurationId { get; set; }

    public TaxConfiguration? TaxConfiguration { get; set; }

    // Total of all served/ordered items before taxes
    [Range(0, double.MaxValue)]
    public decimal FoodTotal { get; set; }

    // Snapshot amounts at bill generation time
    [Range(0, double.MaxValue)]
    public decimal CgstAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SgstAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ServiceChargeAmount { get; set; }
    // Final payable amount after applying your rounding rule
    [Range(0, double.MaxValue)]
    public decimal GrandTotal { get; set; }

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public PaymentMethod? PaymentMethod { get; set; }

    public DateTime GeneratedAt { get; set; }

    public DateTime? PaidAt { get; set; }
}