using System.ComponentModel.DataAnnotations;

namespace RestaurantAPI.Models;

public class TaxConfiguration
{
    public int Id { get; set; }

    [Range(0, 100)]
    [Required(ErrorMessage="CgstPercentage is required")]
    public decimal CgstPercentage { get; set; }

    [Range(0, 100)]
    [Required(ErrorMessage="SgstPercentage is required")]
    public decimal SgstPercentage { get; set; }

    [Range(0, 100)]
    [Required(ErrorMessage="ServiceChargePercentage is required")]
    public decimal ServiceChargePercentage { get; set; }
    [Required(ErrorMessage="EffectiveFrom is required")]
    public DateTime EffectiveFrom { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
}