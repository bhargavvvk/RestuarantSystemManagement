using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.Models;

public class Category
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(15, ErrorMessage = "Category name cannot exceed 15 characters.")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsAvailable { get; set; } = true;

    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    public ICollection<MenuItem>? MenuItems { get; set; }
}