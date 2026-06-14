using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.Models;

public class CartItem
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Cart is required.")]
    public int CartId { get; set; }

    public Cart? Cart { get; set; }

    [Required(ErrorMessage = "Menu item is required.")]
    public int MenuItemId { get; set; }

    public MenuItem? MenuItem { get; set; }
    [Required(ErrorMessage="Quantity is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
    public int Quantity { get; set; }
}