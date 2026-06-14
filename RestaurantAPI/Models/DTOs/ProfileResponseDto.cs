namespace RestaurantAPI.Models.DTOs;

public class ProfileResponseDto
{
    public int Id { get; set; }
    public string Username { get; set; }= string.Empty;
    public string Name { get; set; }= string.Empty;
    public string MobileNumber { get; set; }= string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
}
