namespace RestaurantAPI.Models.DTOs;

public class OrderDetailsDto
{
    public int OrderId { get; set; }
    public string OrderNumber {get; set;}=string.Empty;

    public string TableNumber { get; set; } = string.Empty;

    public DateTime PlacedAt { get; set; }

    public string Status { get; set; } = string.Empty;

    public string BillNumber { get; set; } = string.Empty;

    public decimal BillTotal { get; set; }

    public PaymentMethod? PaymentMethod { get; set; }

    public ICollection<OrderItemSummaryDto> Items { get; set; }
        = new List<OrderItemSummaryDto>();
}