using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.Models;

public class Customer
{
    public int Id { get; set; }
    [Required]
    [StringLength(15)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Encrypted mobile number is required.")]
    public string EncryptedMobileNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile number hash is required.")]
    public string MobileNumberHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public ICollection<DiningSession>? DiningSessions { get; set; }
}