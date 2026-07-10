using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.Models;

public enum OrderItemStatus
{
    Placed,
    Preparing,
    Ready,
    Served,
    Cancelled
}

public class OrderItem
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Order is required.")]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "Menu item is required.")]
    public int MenuItemId { get; set; }

    [Required(ErrorMessage = "Item name is required.")]
    [StringLength(100, ErrorMessage = "Item name cannot exceed 100 characters.")]
    public string ItemName { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Item price cannot be negative.")]
    public decimal ItemPrice { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
    public int Quantity { get; set; }
    public OrderItemStatus Status { get; set; } = OrderItemStatus.Placed;

    public Order? Order { get; set; }

    public MenuItem? MenuItem { get; set; }

    public int? TableId { get; set; }
    public RestaurantTable? Table { get; set; }
}