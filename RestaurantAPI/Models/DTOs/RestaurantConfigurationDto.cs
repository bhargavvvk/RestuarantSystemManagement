using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace RestaurantAPI.Models.DTOs;

/// <summary>Response for all restaurant details including parsed KnowledgeBase.</summary>
public class RestaurantConfigurationResponseDto
{
    public int Id { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? OpeningTime { get; set; }   // "HH:mm" string — easy for frontend
    public string? ClosingTime { get; set; }
    public string? About { get; set; }
    public JsonNode? KnowledgeBase { get; set; }  // parsed JSON, not a raw string
    public DateTime LastUpdatedAt { get; set; }
}

/// <summary>Request to update restaurant details (KnowledgeBase excluded).</summary>
public class UpdateRestaurantDetailsDto
{
    [Required(ErrorMessage = "Restaurant name is required.")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string RestaurantName { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    [MaxLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
    public string? Address { get; set; }

    [MaxLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
    public string? PhoneNumber { get; set; }

    [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string? Email { get; set; }

    /// <summary>"HH:mm" 24-hour format, e.g. "09:00"</summary>
    public string? OpeningTime { get; set; }

    /// <summary>"HH:mm" 24-hour format, e.g. "22:30"</summary>
    public string? ClosingTime { get; set; }

    [MaxLength(2000, ErrorMessage = "About cannot exceed 2000 characters.")]
    public string? About { get; set; }
}

/// <summary>
/// Request to replace the entire KnowledgeBase JSON.
/// The Value property must be a valid JSON object or array.
/// Send null to clear.
/// </summary>
public class UpdateKnowledgeBaseDto
{
    public JsonNode? Value { get; set; }
}
