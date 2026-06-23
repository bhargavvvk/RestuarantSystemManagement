using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Services;

public class MenuService : IMenuService
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ILogger<MenuService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMapper _mapper;
    private readonly IAuditService _auditService;
    private readonly ICategoryRepository _categoryRepository;
    private readonly RestaurantContext _context;
    public MenuService(IMenuItemRepository menuItemRepository,ILogger<MenuService> logger,IHttpContextAccessor httpContextAccessor,
    IMapper mapper,IAuditService auditService,ICategoryRepository categoryRepository,RestaurantContext context,IWebHostEnvironment webHostEnvironment)
    {
        _menuItemRepository = menuItemRepository;
        _logger = logger;
        _mapper = mapper;
        _auditService = auditService;
        _categoryRepository = categoryRepository;
        _context = context;
        _webHostEnvironment = webHostEnvironment;
        _httpContextAccessor=httpContextAccessor;
    }
    public async Task ToggleMenuAvailability(int menuItemId,bool isAvailable)
    {
        _logger.LogInformation("Toggling menu item {MenuItemId} availability to {IsAvailable}", menuItemId, isAvailable);
        var item =await _menuItemRepository.Get(menuItemId);
    if(item == null)
    {
        throw new MenuItemNotFoundException();
    }
        var oldValues = new
    {
        item.IsAvailable
    };

        item.IsAvailable = isAvailable;
        await _auditService.LogAsync(
        nameof(item),
        item.Id.ToString(),
        AuditAction.Updated,
        oldValues,
        new
        {
            item.IsAvailable
        },
        isAvailable
            ? "Menu item enabled"
            : "Menu item disabled");
    await _menuItemRepository.SaveChangesAsync();
        _logger.LogInformation("Menu item {MenuItemId} availability set to {IsAvailable}", menuItemId, isAvailable);
    }

    public async Task ToggleCategoryAvailability(int categoryId, bool isAvailable)
    {
        _logger.LogInformation("Toggling category {CategoryId} availability to {IsAvailable}", categoryId, isAvailable);
        var category =await _categoryRepository.Get(categoryId);
        if(category == null)
        {
            throw new CategoryNotFoundException("Category Not found");
        }
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var oldValues = new
            {
                category.IsAvailable
            };
            category.IsAvailable = isAvailable;
            await _categoryRepository.SaveChangesAsync();
            if(!isAvailable)
            {
                _logger.LogInformation("Making menu items unavailable");
                foreach(var item in category.MenuItems!)
                {
                    item.IsAvailable = false;
                }
                await _menuItemRepository.SaveChangesAsync();
            }
            await _auditService.LogAsync(
            nameof(Category),
            category.Id.ToString(),
            AuditAction.Updated,
            oldValues,
            new
            {
                category.IsAvailable,
                AffectedMenuItems =category.MenuItems!.Count
            },
            $"Category disabled. {category.MenuItems!.Count} menu items automatically marked unavailable");
            await _menuItemRepository.SaveChangesAsync();
            await transaction.CommitAsync();
            _logger.LogInformation("Category {CategoryId} availability set to {IsAvailable}", categoryId, isAvailable);
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            _logger.LogError(e,"Error while changing category availability");
            throw;
        }
    }
    public async Task<MenuItemResponseDto> AddMenuItem(
    AddMenuItemDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new Exception( "Menu item name is required");
        }
        if(request.Name.Length > 15)
        {
            throw new Exception(" name cannot exceed 15 characters");
        }
        if (request.Price <= 0)
        {
            throw new Exception( "Price must be greater than zero");
        }
         if (request.Description?.Length > 100)
        {
            throw new Exception("Description cannot exceed 100 characters");
        }
        var category =await _categoryRepository.Get(request.CategoryId);

        if (category == null)
        {
            throw new CategoryNotFoundException("Category not found");
        }

        var existingItem =await _menuItemRepository.GetByName(request.Name.Trim());

        if (existingItem != null)
        {
            throw new Exception("Menu item already exists");
        }

        string? imagePath = null;

        if (request.Image != null &&request.Image.Length > 0)
        {
            var folderPath =Path.Combine(_webHostEnvironment.WebRootPath,"images","menu");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            var fileName =$"{Guid.NewGuid()}" +$"{Path.GetExtension(request.Image.FileName)}";
            var filePath =Path.Combine(folderPath,fileName);
            await using var stream =new FileStream(filePath,FileMode.Create);
            await request.Image.CopyToAsync(stream);
            imagePath =$"/images/menu/{fileName}";
        }
        var menuItem = new MenuItem
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            Price = request.Price,
            CategoryId = request.CategoryId,
            ImageUrl = imagePath,
            IsAvailable =category.IsAvailable
                    ? request.IsAvailable
                    : false
        };
        await _menuItemRepository.Create(menuItem);
        await _menuItemRepository.SaveChangesAsync();
        await _auditService.LogAsync(
            nameof(MenuItem),
            menuItem.Id.ToString(),
            AuditAction.Created,
            null,
            new
            {
                menuItem.Name,
                menuItem.Price,
                menuItem.CategoryId,
                menuItem.IsAvailable
            },
            "Menu item created");

        await _menuItemRepository.SaveChangesAsync();

        _logger.LogInformation("Menu item {MenuItemId} created",menuItem.Id);
        var result = _mapper.Map<MenuItemResponseDto>(menuItem);
        if (!string.IsNullOrEmpty(result.ImageUrl))
            result.ImageUrl = BuildAbsoluteUrl(result.ImageUrl);
        return result;
    }
    public async Task<CategoryResponseDto>AddCategory(AddCategoryDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new Exception("Category name is required");
        }
        if(request.Name.Length > 15)
        {
            throw new Exception("Category name cannot exceed 50 characters");
        }
            var existingCategory =await _categoryRepository.GetByName(request.Name.Trim());
        if (existingCategory != null)
        {
            throw new Exception("Category already exists");
        }
         if (request.Description?.Length > 100)
        {
            throw new Exception("Description cannot exceed 100 characters");
        }
        var category = new Category
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            IsAvailable = request.IsAvailable
        };

        await using var transaction =await _context.Database.BeginTransactionAsync();

        try
        {
            await _categoryRepository.Create(category);
            await _categoryRepository.SaveChangesAsync();
            await _auditService.LogAsync(
                nameof(Category),
                category.Id.ToString(),
                AuditAction.Created,
                null,
                new
                {
                    category.Name,
                    category.IsAvailable
                },
                "Category created");
            await _categoryRepository.SaveChangesAsync();
            await transaction.CommitAsync();
            _logger.LogInformation("Category {CategoryId} '{CategoryName}' created", category.Id, category.Name);
            return _mapper.Map<CategoryResponseDto>(category);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<MenuItemResponseDto> UpdateMenuItem(int menuItemId,UpdateMenuItemDto request)
    {
        var menuItem =await _menuItemRepository.Get(menuItemId);
        if (menuItem == null)
        {
            throw new MenuItemNotFoundException();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new Exception("Menu item name is required");
        }
        if (request.Name.Length > 15)
        {
            throw new Exception("Menu item name cannot exceed 15 characters");
        }

        if (request.Price <= 0)
        {
            throw new Exception("Price must be greater than zero");
        }
        if (request.Description?.Length > 100)
        {
            throw new Exception("Description cannot exceed 100 characters");
        }

        var category =await _categoryRepository.Get(request.CategoryId);
        if (category == null)
        {
            throw new CategoryNotFoundException("Category Not found");
        }

        var existingItem =await _menuItemRepository.GetByName(request.Name.Trim());
        if (existingItem != null && existingItem.Id != menuItemId)
        {
            throw new Exception("Menu item already exists");
        }
        var oldValues = new
        {
            menuItem.Name,
            menuItem.Price,
            menuItem.CategoryId,
            menuItem.Description,
            menuItem.IsAvailable
        };
        menuItem.Name =request.Name.Trim();
        menuItem.Price =request.Price;
        menuItem.Description =request.Description;
        menuItem.CategoryId =request.CategoryId;
        if (!category.IsAvailable)
        {
            menuItem.IsAvailable = false;
        }

        await using var transaction =await _context.Database.BeginTransactionAsync();

        try
        {
            await _menuItemRepository.SaveChangesAsync();
            await _auditService.LogAsync(
                nameof(MenuItem),
                menuItem.Id.ToString(),
                AuditAction.Updated,
                oldValues,
                new
                {
                    menuItem.Name,
                    menuItem.Price,
                    menuItem.CategoryId,
                    menuItem.Description,
                    menuItem.IsAvailable
                },
                !category.IsAvailable
                    ? "Menu item updated and automatically marked unavailable because category is unavailable"
                    : "Menu item updated");

            await _menuItemRepository.SaveChangesAsync();
            await transaction.CommitAsync();
            _logger.LogInformation("Menu item {MenuItemId} updated",menuItem.Id);
            var result = _mapper.Map<MenuItemResponseDto>(menuItem);
            if (!string.IsNullOrEmpty(result.ImageUrl))
                result.ImageUrl = BuildAbsoluteUrl(result.ImageUrl);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    public async Task<CategoryResponseDto> UpdateCategory(int categoryId,UpdateCategoryDto request)
    {
        var category = await _categoryRepository.Get(categoryId);
        if (category == null)
        {
            throw new CategoryNotFoundException("category not found");
        }
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new Exception("Category name is required");
        }
        if (request.Name.Length > 15)
        {
            throw new Exception("Category name cannot exceed 15 characters");
        }
        if (request.Description?.Length > 100)
        {
            throw new Exception("Description cannot exceed 100 characters");
        }
        var existingCategory =await _categoryRepository.GetByName(request.Name.Trim());
        if (existingCategory != null && existingCategory.Id != categoryId)
        {
            throw new Exception("Category already exists");
        }
        var oldValues = new
        {
            category.Name,
            category.Description
        };
        category.Name =request.Name.Trim();
        category.Description =request.Description;
        await using var transaction =await _context.Database.BeginTransactionAsync();
        try
        {
            await _categoryRepository.SaveChangesAsync();
            await _auditService.LogAsync(
                nameof(Category),
                category.Id.ToString(),
                AuditAction.Updated,
                oldValues,
                new
                {
                    category.Name,
                    category.Description
                },
                "Category updated");

            await _categoryRepository.SaveChangesAsync();
            await transaction.CommitAsync();
            _logger.LogInformation(
                "Category {CategoryId} updated",
                category.Id);
            return _mapper.Map<CategoryResponseDto>(category);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    public async Task DeleteMenuItem(int menuItemId)
    {
        var menuItem =await _menuItemRepository.Get(menuItemId);
        if (menuItem == null)
        {
            throw new MenuItemNotFoundException();
        }

        if (menuItem.IsDeleted)
        {
            throw new Exception("Menu item already deleted");
        }
        var oldValues = new
        {
            menuItem.IsDeleted,
            menuItem.IsAvailable
        };
        await using var transaction =await _context.Database.BeginTransactionAsync();
        try
        {
            menuItem.IsDeleted = true;
            menuItem.IsAvailable=false;
            await _menuItemRepository.SaveChangesAsync();
            await _auditService.LogAsync(
                nameof(MenuItem),
                menuItem.Id.ToString(),
                AuditAction.Deleted,
                oldValues,
                new
                {
                    menuItem.IsDeleted
                },
                "Menu item deleted");

            await _menuItemRepository
                .SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("Menu item {MenuItemId} deleted",menuItem.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    public async Task DeleteCategory(int categoryId)
    {
        _logger.LogInformation("Deleting category {CategoryId}", categoryId);
        var category =await _categoryRepository.Get(categoryId);
        if (category == null)
        {
            throw new CategoryNotFoundException("Category Not found");
        }
        if (category.IsDeleted)
        {
            throw new Exception("Category already deleted");
        }
        var oldValues = new
        {
            category.IsDeleted,
            category.IsAvailable
        };

        await using var transaction =await _context.Database.BeginTransactionAsync();
        try
        {
            category.IsDeleted = true;
            category.IsAvailable = false;
            foreach (var menuItem in category.MenuItems!)
            {
                menuItem.IsDeleted = true;
                menuItem.IsAvailable=false;
            }
            await _categoryRepository.SaveChangesAsync();
            await _auditService.LogAsync(
                nameof(Category),
                category.Id.ToString(),
                AuditAction.Deleted,
                oldValues,
                new
                {
                    category.IsDeleted
                },
                $"Category deleted. {category.MenuItems.Count} menu items were also soft deleted.");

            await _categoryRepository.SaveChangesAsync();
            await transaction.CommitAsync();
            _logger.LogInformation("Category {CategoryId} and {MenuItemCount} menu items deleted", categoryId, category.MenuItems.Count);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

    }
    public async Task<ICollection<MenuItemResponseDto>>GetMenu(string? search,int? categoryId,bool? isAvailable,FoodType? foodType)
    {
        _logger.LogInformation("Fetching menu items");
        var query =_menuItemRepository.GetMenuQuery();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch =search.Trim().ToUpper();
            query = query.Where(m =>m.Name.ToUpper().Contains(normalizedSearch));
        }
        if (categoryId.HasValue)
        {
            query = query.Where(m =>m.CategoryId ==categoryId.Value);
        }
        if (foodType.HasValue)
        {
            query = query.Where(m =>m.FoodType ==foodType.Value);
        }
        if (isAvailable.HasValue)
        {
            query = query.Where(m =>m.IsAvailable ==isAvailable.Value);
        }
        var menuItems =await query.OrderBy(m => m.Name).ToListAsync();
        var result = _mapper.Map<ICollection<MenuItemResponseDto>>(menuItems);
        foreach (var item in result)
        {
            if (!string.IsNullOrEmpty(item.ImageUrl))
                item.ImageUrl = BuildAbsoluteUrl(item.ImageUrl);
        }
        return result;
    }
    public async Task<ICollection<CategoryResponseDto>> GetCategories()
    {
        var categories =await _categoryRepository.GetAll();
        return categories.Select(c =>
                new CategoryResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    IsAvailable=c.IsAvailable
                })
            .ToList();
    }

    private string BuildAbsoluteUrl(string path)
    {
        var request = _httpContextAccessor.HttpContext!.Request;
        return $"{request.Scheme}://{request.Host}{path}";
    }
}