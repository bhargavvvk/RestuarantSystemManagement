namespace RestaurantAPI.Models.DTOs;

public class OrderCancelNotificationDto
{
    public string TableNumber { get; set; }=string.Empty;
    public string OrderNumber { get; set; }=string.Empty;
    public string Message {get;set;}=string.Empty;
}
