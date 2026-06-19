using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;

namespace RestaurantAPI.ServiceInterfaces;

public interface IMenuService
{
    Task<ICollection<MenuItemResponseDto>>GetMenu(string? search,int? categoryId,bool? isAvailable,FoodType? foodType);
    Task ToggleMenuAvailability(int menuItemId, bool isAvailable);
    Task ToggleCategoryAvailability(int categoryId, bool isAvailable);
    Task<MenuItemResponseDto> AddMenuItem(AddMenuItemDto request);
    Task<CategoryResponseDto> AddCategory(AddCategoryDto request);
    Task<MenuItemResponseDto> UpdateMenuItem(int menuItemId,UpdateMenuItemDto request);
    Task<CategoryResponseDto> UpdateCategory(int categoryId,UpdateCategoryDto request);
    Task DeleteMenuItem(int menuItemId);
    Task DeleteCategory(int categoryId);
    Task<ICollection<CategoryResponseDto>> GetCategories();
}
