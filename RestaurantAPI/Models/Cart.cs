using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.Models;

public class Cart
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Dining session is required.")]
    public int DiningSessionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DiningSession? DiningSession { get; set; }
    public ICollection<CartItem>? CartItems { get; set; }
}