namespace RestaurantAPI.Models.DTOs;

public class BillRegistryDto
{
    public int BillId { get; set; }

    public string BillNumber { get; set; } = string.Empty;

    public string TableNumber { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; }

    public decimal GrandTotal { get; set; }
}
