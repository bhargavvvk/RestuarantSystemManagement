namespace RestaurantAPI.Models.DTOs;

public class CreateWaiterRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
}
