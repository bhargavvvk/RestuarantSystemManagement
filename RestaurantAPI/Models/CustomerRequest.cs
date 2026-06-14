using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.Models;

public enum CustomerRequestType
{
    CallWaiter,
    NeedWater,
    RequestBill
}

public enum CustomerRequestStatus
{
    Pending,
    Completed
}

public class CustomerRequest
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Dining session is required.")]
    public int DiningSessionId { get; set; }

    public DiningSession? DiningSession { get; set; }

    public CustomerRequestType RequestType { get; set; }

    public CustomerRequestStatus Status { get; set; } = CustomerRequestStatus.Pending;

    public DateTime RequestedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
