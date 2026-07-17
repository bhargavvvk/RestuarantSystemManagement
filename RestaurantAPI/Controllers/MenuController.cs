using Microsoft.AspNetCore.Mvc;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenuController : ControllerBase
{
    private readonly IMenuService _menuService;
    private readonly IIngredientService _ingredientService;

    public MenuController(IMenuService menuService, IIngredientService ingredientService)
    {
        _menuService = menuService;
        _ingredientService = ingredientService;
    }

    [HttpGet]
    public async Task<ActionResult<ICollection<MenuItemResponseDto>>> GetMenu(
            [FromQuery] string? search,
            [FromQuery] int? categoryId,
            [FromQuery] bool? isAvailable,
            [FromQuery] FoodType? foodType)
    {
        var result = await _menuService.GetMenu(search, categoryId, isAvailable, foodType);
        return Ok(result);
    }

    [HttpGet("categories")]
    public async Task<ActionResult<ICollection<CategoryResponseDto>>> GetCategories()
    {
        return Ok(await _menuService.GetCategories());
    }

    /// <summary>
    /// GET api/menu/{menuItemId}/ingredients
    /// Public — returns the ingredient list for a menu item (useful for allergen info).
    /// Returns an empty array if no ingredients have been set.
    /// </summary>
    [HttpGet("{menuItemId:int}/ingredients")]
    public async Task<ActionResult<ICollection<MenuItemIngredientResponseDto>>> GetMenuItemIngredients(
        int menuItemId)
    {
        var result = await _ingredientService.GetMenuItemIngredients(menuItemId);
        return Ok(result);
    }

    /// <summary>
    /// GET api/menu/{menuItemId}/nutrition
    /// Public — returns nutrition info for a menu item.
    /// Returns 404 if none has been set.
    /// </summary>
    [HttpGet("{menuItemId:int}/nutrition")]
    public async Task<ActionResult<MenuItemNutritionResponseDto>> GetMenuItemNutrition(int menuItemId)
    {
        var result = await _ingredientService.GetMenuItemNutrition(menuItemId);
        if (result == null)
            return NotFound(new { message = "No nutrition information available for this menu item." });

        return Ok(result);
    }
}
