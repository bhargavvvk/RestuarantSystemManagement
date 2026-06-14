namespace RestaurantAPI.Models.DTOs;

public class WaiterManagementResponseDto
{
    public WaiterSummaryDto Summary { get; set; } = null!;

    public ICollection<WaiterCardDto> Waiters { get; set; }
        = new List<WaiterCardDto>();
}