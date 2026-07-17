using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.Models;

public class RestaurantConfiguration
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string RestaurantName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }
    public TimeSpan? OpeningTime { get; set; }

    public TimeSpan? ClosingTime { get; set; }

    [MaxLength(2000)]
    public string? About { get; set; }

    /// <summary>
    /// JSON containing FAQs, policies, holiday timings,
    /// special services, etc.
    /// </summary>
    public string? KnowledgeBase { get; set; }
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}