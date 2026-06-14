namespace RestaurantAPI.Models.DTOs;

public class CreateSessionRequestDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}
