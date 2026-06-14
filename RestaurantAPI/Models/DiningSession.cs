using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.Models;

public enum DiningSessionStatus
{
    Active,
    Completed
}

public class DiningSession
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Table is required.")]
    public int TableId { get; set; }

    public RestaurantTable? Table { get; set; }

    [Required(ErrorMessage = "Customer is required.")]
    public int CustomerId { get; set; }

    public Customer? Customer { get; set; }

   [Required(ErrorMessage = "Waiter is required.")]
    public int WaiterId { get; set; }

    public User? Waiter { get; set; }

    [Required(ErrorMessage = "Session OTP is required.")]
    [StringLength(4, MinimumLength = 4)]
    public string SessionOtp { get; set; } = string.Empty;

    public DiningSessionStatus Status { get; set; } = DiningSessionStatus.Active;

    public DateTime StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public Cart? Cart { get; set; }

    public ICollection<Order>? Orders { get; set; }

    public ICollection<CustomerRequest>? CustomerRequests { get; set; }

    public Bill? Bill { get; set; }
}