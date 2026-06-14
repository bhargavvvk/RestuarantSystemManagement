namespace RestaurantAPI.Models.DTOs;

public class CreateSessionResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string SessionOtp { get; set; } = string.Empty;
}
