namespace RestaurantAPI.Models.DTOs;

public class TableDashboardDto
{
    public int Id { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
