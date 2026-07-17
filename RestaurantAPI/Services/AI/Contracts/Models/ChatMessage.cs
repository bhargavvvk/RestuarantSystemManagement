namespace RestaurantAPI.Services.AI.Contracts.Models;
public class ChatMessage
{
   public ChatRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}