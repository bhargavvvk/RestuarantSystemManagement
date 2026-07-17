using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Controllers;

/// <summary>
/// Admin-only endpoints for managing ingredients, menu-item ingredient lists,
/// and menu-item nutrition information.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminIngredientController : ControllerBase
{
    private readonly IIngredientService _ingredientService;

    public AdminIngredientController(IIngredientService ingredientService)
    {
        _ingredientService = ingredientService;
    }

    // ── Ingredient master list ────────────────────────────────────────────────

    /// <summary>
    /// GET api/admin/ingredients
    /// Returns all active ingredients. Optionally filter by name via ?search=
    /// </summary>
    [HttpGet("ingredients")]
    public async Task<ActionResult<ICollection<IngredientResponseDto>>> GetIngredients(
        [FromQuery] string? search)
    {
        var result = await _ingredientService.GetIngredients(search);
        return Ok(result);
    }

    /// <summary>
    /// GET api/admin/ingredients/{id}
    /// Returns a single ingredient by ID.
    /// </summary>
    [HttpGet("ingredients/{id:int}")]
    public async Task<ActionResult<IngredientResponseDto>> GetIngredient(int id)
    {
        var result = await _ingredientService.GetIngredient(id);
        return Ok(result);
    }

    /// <summary>
    /// POST api/admin/ingredients
    /// Creates a new ingredient in the master list.
    /// </summary>
    [HttpPost("ingredients")]
    public async Task<ActionResult<IngredientResponseDto>> AddIngredient(
        [FromBody] AddIngredientDto request)
    {
        var result = await _ingredientService.AddIngredient(request);
        return CreatedAtAction(nameof(GetIngredient), new { id = result.Id }, result);
    }

    /// <summary>
    /// PUT api/admin/ingredients/{id}
    /// Updates an existing ingredient's name / description.
    /// </summary>
    [HttpPut("ingredients/{id:int}")]
    public async Task<ActionResult<IngredientResponseDto>> UpdateIngredient(
        int id, [FromBody] UpdateIngredientDto request)
    {
        var result = await _ingredientService.UpdateIngredient(id, request);
        return Ok(result);
    }

    /// <summary>
    /// DELETE api/admin/ingredients/{id}
    /// Soft-deletes an ingredient. Fails if the ingredient is still used by any menu item.
    /// </summary>
    [HttpDelete("ingredients/{id:int}")]
    public async Task<IActionResult> DeleteIngredient(int id)
    {
        await _ingredientService.DeleteIngredient(id);
        return NoContent();
    }

    // ── Menu-item ingredients ────────────────────────────────────────────────

    /// <summary>
    /// GET api/admin/menu/{menuItemId}/ingredients
    /// Returns the ingredient list for a specific menu item.
    /// </summary>
    [HttpGet("menu/{menuItemId:int}/ingredients")]
    public async Task<ActionResult<ICollection<MenuItemIngredientResponseDto>>> GetMenuItemIngredients(
        int menuItemId)
    {
        var result = await _ingredientService.GetMenuItemIngredients(menuItemId);
        return Ok(result);
    }

    /// <summary>
    /// PUT api/admin/menu/{menuItemId}/ingredients
    /// Full-replace the ingredient list for a menu item.
    /// Send an empty Ingredients array to clear all ingredients.
    /// Each entry must supply either IngredientId (existing) or NewIngredient (inline create), not both.
    /// </summary>
    [HttpPut("menu/{menuItemId:int}/ingredients")]
    public async Task<ActionResult<ICollection<MenuItemIngredientResponseDto>>> SetMenuItemIngredients(
        int menuItemId, [FromBody] SetMenuItemIngredientsDto request)
    {
        var result = await _ingredientService.SetMenuItemIngredients(menuItemId, request);
        return Ok(result);
    }

    // ── Menu-item nutrition ──────────────────────────────────────────────────

    /// <summary>
    /// GET api/admin/menu/{menuItemId}/nutrition
    /// Returns nutrition info for a menu item. Returns 404 if none has been set.
    /// </summary>
    [HttpGet("menu/{menuItemId:int}/nutrition")]
    public async Task<ActionResult<MenuItemNutritionResponseDto>> GetMenuItemNutrition(int menuItemId)
    {
        var result = await _ingredientService.GetMenuItemNutrition(menuItemId);
        if (result == null)
            return NotFound(new { message = "No nutrition information has been set for this menu item." });

        return Ok(result);
    }

    /// <summary>
    /// PUT api/admin/menu/{menuItemId}/nutrition
    /// Upserts (creates or replaces) the nutrition info for a menu item.
    /// All numeric fields are optional but must be >= 0 when provided.
    /// </summary>
    [HttpPut("menu/{menuItemId:int}/nutrition")]
    public async Task<ActionResult<MenuItemNutritionResponseDto>> SetMenuItemNutrition(
        int menuItemId, [FromBody] MenuItemNutritionDto request)
    {
        var result = await _ingredientService.SetMenuItemNutrition(menuItemId, request);
        return Ok(result);
    }

    /// <summary>
    /// DELETE api/admin/menu/{menuItemId}/nutrition
    /// Removes all nutrition info for a menu item.
    /// </summary>
    [HttpDelete("menu/{menuItemId:int}/nutrition")]
    public async Task<IActionResult> DeleteMenuItemNutrition(int menuItemId)
    {
        await _ingredientService.DeleteMenuItemNutrition(menuItemId);
        return NoContent();
    }
}
