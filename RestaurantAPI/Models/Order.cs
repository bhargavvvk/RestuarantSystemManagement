using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.Models;
public class Order
{
    public int Id { get; set; }
     [Required(ErrorMessage = "OrderNumber is required.")]
    public string OrderNumber { get; set; }
    = string.Empty;

    [Required(ErrorMessage = "Dining session is required.")]
    public int DiningSessionId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Total amount cannot be negative.")]
    public decimal TotalAmount { get; set; }

    public DateTime PlacedAt { get; set; }

    public DateTime? CancelledAt { get; set; }
    
    [StringLength(100, ErrorMessage = "Instructions cannot exceed 100 characters.")]
    public string? SpecialInstructions { get; set; }
    public ICollection<OrderItem>? OrderItems { get; set; }

    public DiningSession? DiningSession { get; set; }
}