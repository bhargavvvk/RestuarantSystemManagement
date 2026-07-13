using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.Models.DTOs;

public class MarkTableSplitPaidDto
{
    [Required]
    public int TargetTableId { get; set; }

    [Required]
    public PaymentMethod PaymentMethod { get; set; }
}
