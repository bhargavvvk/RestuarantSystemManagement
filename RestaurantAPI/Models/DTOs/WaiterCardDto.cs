namespace RestaurantAPI.Models.DTOs;

public class WaiterCardDto
{
    public int WaiterId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int AssignedTableCount { get; set; }

    public ICollection<string> AssignedTables { get; set; }
        = new List<string>();
}
