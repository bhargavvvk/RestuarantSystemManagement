namespace RestaurantAPI.Models.DTOs;

public class JoinSessionResponseDto
{
    public string Token { get; set; } = string.Empty;
     public string SessionOtp { get; set; } = string.Empty;
}
