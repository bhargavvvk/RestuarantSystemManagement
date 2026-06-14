using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.Models;

public enum UserRole
{
    Admin,
    Waiter,
    KitchenStaff
}

public class User
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Username is required.")]
    [StringLength(15, ErrorMessage = "Username cannot exceed 15 characters.")]
    public string Username { get; set; } = string.Empty;
    [Required(ErrorMessage = "name is required.")]
    [StringLength(15, ErrorMessage = "Name cannot exceed 15 characters.")]

    public string Name { get; set; } = string.Empty;
    [Required(ErrorMessage = "Encrypted mobile number is required.")]
    public string EncryptedMobileNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile number hash is required.")]
    public string MobileNumberHash { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    [Required(ErrorMessage = "Password hash is required.")]
    public byte[] PasswordHash { get; set; } = [];
    public byte[] HashKey { get; set; }=[];
    [Required(ErrorMessage = "User role is required.")]
    public UserRole Role { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public bool MustChangePassword { get; set; } = true;
    public ICollection<DiningSession>? DiningSessions { get; set; }
}