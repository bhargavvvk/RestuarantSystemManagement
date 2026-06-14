using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.Models;

public class InventoryItem
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Inventory item name is required.")]
    [StringLength(20, ErrorMessage = "Inventory item name cannot exceed 20 characters.")]
    public string Name { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Available quantity cannot be negative.")]
    public decimal AvailableQuantity { get; set; }

    [Required(ErrorMessage = "Unit is required.")]
    public string Unit { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Minimum stock threshold cannot be negative.")]
    public decimal MinimumStockThreshold { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime LastUpdatedAt { get; set; }
}