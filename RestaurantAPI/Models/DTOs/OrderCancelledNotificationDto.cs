namespace RestaurantAPI.Models.DTOs;

public class OrderCancelledNotificationDto
{
    public int OrderId { get; set; }

    public string TableNumber { get; set; }
        = string.Empty;

    public string Message { get; set; }= string.Empty;
}
