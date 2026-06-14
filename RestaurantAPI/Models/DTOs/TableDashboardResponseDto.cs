namespace RestaurantAPI.Models.DTOs;

public class TableDashboardResponseDto
{
    public TableDashboardCountsDto Summary { get; set; } = null!;

    public ICollection<TableDashboardDto> Tables { get; set; }
        = new List<TableDashboardDto>();
}
