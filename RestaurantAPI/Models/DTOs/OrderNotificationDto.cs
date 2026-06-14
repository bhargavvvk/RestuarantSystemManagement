namespace RestaurantAPI.Models.DTOs;

public class OrderNotificationDto
{
    public string TableNumber { get; set; }=string.Empty;
    public string Message {get; set;}=string.Empty;
}
