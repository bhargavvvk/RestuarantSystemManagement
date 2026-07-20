namespace RestaurantAPI.Models.DTOs;

public class WaiterTableResponseDto
{
    public int TableId { get; set; }

    public string TableNumber { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int? SessionId { get; set; }

    /// <summary>OTP of the active dining session. Null when the table has no active session.</summary>
    public string? SessionOtp { get; set; }
}
