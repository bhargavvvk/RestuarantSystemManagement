namespace RestaurantAPI.Models.DTOs;

public class CreateSystemUsersRequestDto
{
    public string AdminMobileNumber { get; set; } = string.Empty;
    public string KitchenMobileNumber { get; set; } = string.Empty;
}
