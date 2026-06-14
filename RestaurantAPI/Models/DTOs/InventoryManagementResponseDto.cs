namespace RestaurantAPI.Models.DTOs;

public class InventoryManagementResponseDto
{
    public InventorySummaryDto Summary { get; set; } = null!;

    public PagedResponseDto<InventoryItemCardDto> Items { get; set; } = null!;
}
