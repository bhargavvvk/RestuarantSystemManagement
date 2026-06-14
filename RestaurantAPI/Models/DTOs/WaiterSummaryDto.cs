namespace RestaurantAPI.Models.DTOs;

public class WaiterSummaryDto
{
    public int TotalWaiters { get; set; }

    public int ActiveWaiters { get; set; }

    public int InactiveWaiters { get; set; }
}
