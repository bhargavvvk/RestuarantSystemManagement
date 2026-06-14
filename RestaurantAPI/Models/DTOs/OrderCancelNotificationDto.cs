namespace RestaurantAPI.Models.DTOs;

public class OrderCancelNotificationDto
{
    public string TableNumber { get; set; }=string.Empty;
    public int OrderId { get; set; }
    public string Message {get;set;}=string.Empty;
}
