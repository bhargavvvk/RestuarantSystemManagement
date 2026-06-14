namespace RestaurantAPI.Models.DTOs;
public class CustomerTokenRequest
{
    public int TableId { get; set; }
    public int SessionId { get; set; }
    public int WaiterId { get; set; }
    public int CartId { get; set; }
}
