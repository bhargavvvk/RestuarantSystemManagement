using RestaurantAPI.Models.DTOs;

namespace RestaurantAPI.ServiceInterfaces;

public interface IIngredientService
{
    // ── Ingredient CRUD ──────────────────────────────────────────────────────
    Task<ICollection<IngredientResponseDto>> GetIngredients(string? search);
    Task<IngredientResponseDto> GetIngredient(int id);
    Task<IngredientResponseDto> AddIngredient(AddIngredientDto request);
    Task<IngredientResponseDto> UpdateIngredient(int id, UpdateIngredientDto request);
    Task DeleteIngredient(int id);

    // ── Menu-item ingredients (full replace) ─────────────────────────────────
    Task<ICollection<MenuItemIngredientResponseDto>> GetMenuItemIngredients(int menuItemId);
    Task<ICollection<MenuItemIngredientResponseDto>> SetMenuItemIngredients(int menuItemId, SetMenuItemIngredientsDto request);

    // ── Menu-item nutrition (upsert / delete) ────────────────────────────────
    Task<MenuItemNutritionResponseDto?> GetMenuItemNutrition(int menuItemId);
    Task<MenuItemNutritionResponseDto> SetMenuItemNutrition(int menuItemId, MenuItemNutritionDto request);
    Task DeleteMenuItemNutrition(int menuItemId);
}
