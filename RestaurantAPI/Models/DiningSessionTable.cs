using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.Models;

public class DiningSessionTable
{
    public int Id { get; set; }

    [Required]
    public int DiningSessionId { get; set; }

    public DiningSession? DiningSession { get; set; }

    [Required]
    public int TableId { get; set; }

    public RestaurantTable? Table { get; set; }

    public DateTime LinkedAt { get; set; }
}
