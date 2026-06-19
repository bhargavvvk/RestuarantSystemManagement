namespace RestaurantAPI.Models.DTOs;

public class SessionValidationResponseDto
{
    public bool IsActive { get; set; }
    public string? TableIdentifier { get; set; }
}
