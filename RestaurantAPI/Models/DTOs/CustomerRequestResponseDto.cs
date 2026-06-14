namespace RestaurantAPI.Models.DTOs;

public class CustomerRequestResponseDto
{
    public int RequestId { get; set; }

    public string TableNumber { get; set; } = string.Empty;

    public CustomerRequestType RequestType { get; set; }

    public DateTime RequestedAt { get; set; }
}
