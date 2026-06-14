namespace RestaurantAPI.Models.DTOs;

public class CustomerRequestNotificationDto
{
    public string TableNumber { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
}
