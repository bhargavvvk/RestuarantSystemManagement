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

    public MenuController(IMenuService menuService)
    {
        _menuService = menuService;
    }
    [HttpGet]
    [HttpGet]
    public async Task<ActionResult<ICollection<MenuItemResponseDto>>>GetMenu(
            [FromQuery] string? search,
            [FromQuery] int? categoryId,
            [FromQuery] bool? isAvailable,
            [FromQuery] FoodType? foodType)
    {
        var result =await _menuService.GetMenu(
                search,
                categoryId,
                isAvailable,
                foodType);
        return Ok(result);
    }
    [HttpGet("categories")]
    public async Task<ActionResult<ICollection<CategoryResponseDto>>>GetCategories()
    {
        return Ok(await _menuService.GetCategories());
    }
}
