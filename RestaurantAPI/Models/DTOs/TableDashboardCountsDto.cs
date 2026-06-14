namespace RestaurantAPI.Models.DTOs;

public class TableDashboardCountsDto
{
    public int TotalTables { get; set; }

    public int AvailableTables { get; set; }

    public int OccupiedTables { get; set; }

    public int UnavailableTables { get; set; }
}