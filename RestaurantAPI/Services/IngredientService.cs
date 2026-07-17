using AutoMapper;
using RestaurantAPI.Contexts;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Services;

public class IngredientService : IIngredientService
{
    private readonly IIngredientRepository _ingredientRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IMenuItemIngredientRepository _menuItemIngredientRepository;
    private readonly IMenuItemNutritionRepository _menuItemNutritionRepository;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;
    private readonly ILogger<IngredientService> _logger;
    private readonly RestaurantContext _context;

    public IngredientService(
        IIngredientRepository ingredientRepository,
        IMenuItemRepository menuItemRepository,
        IMenuItemIngredientRepository menuItemIngredientRepository,
        IMenuItemNutritionRepository menuItemNutritionRepository,
        IAuditService auditService,
        IMapper mapper,
        ILogger<IngredientService> logger,
        RestaurantContext context)
    {
        _ingredientRepository = ingredientRepository;
        _menuItemRepository = menuItemRepository;
        _menuItemIngredientRepository = menuItemIngredientRepository;
        _menuItemNutritionRepository = menuItemNutritionRepository;
        _auditService = auditService;
        _mapper = mapper;
        _logger = logger;
        _context = context;
    }

    // ── Ingredient CRUD ──────────────────────────────────────────────────────

    public async Task<ICollection<IngredientResponseDto>> GetIngredients(string? search)
    {
        var ingredients = await _ingredientRepository.Search(search);
        return _mapper.Map<ICollection<IngredientResponseDto>>(ingredients);
    }

    public async Task<IngredientResponseDto> GetIngredient(int id)
    {
        var ingredient = await _ingredientRepository.Get(id)
            ?? throw new IngredientNotFoundException();

        return _mapper.Map<IngredientResponseDto>(ingredient);
    }

    public async Task<IngredientResponseDto> AddIngredient(AddIngredientDto request)
    {
        ValidateIngredientName(request.Name);

        var existing = await _ingredientRepository.GetByName(request.Name.Trim());
        if (existing != null)
            throw new DuplicateEntityException("An ingredient with this name already exists.");

        var ingredient = new Ingredient
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            CreatedAt = DateTime.Now        };

        await _ingredientRepository.Create(ingredient);
        await _ingredientRepository.SaveChangesAsync();

        await _auditService.LogAsync(
            nameof(Ingredient),
            ingredient.Id.ToString(),
            ingredient.Name,
            AuditAction.Created,
            null,
            new { ingredient.Name, ingredient.Description },
            "Ingredient created");

        _logger.LogInformation("Ingredient {IngredientId} '{IngredientName}' created", ingredient.Id, ingredient.Name);
        return _mapper.Map<IngredientResponseDto>(ingredient);
    }

    public async Task<IngredientResponseDto> UpdateIngredient(int id, UpdateIngredientDto request)
    {
        var ingredient = await _ingredientRepository.Get(id)
            ?? throw new IngredientNotFoundException();

        ValidateIngredientName(request.Name);

        var duplicate = await _ingredientRepository.GetByName(request.Name.Trim());
        if (duplicate != null && duplicate.Id != id)
            throw new DuplicateEntityException("An ingredient with this name already exists.");

        var oldValues = new { ingredient.Name, ingredient.Description };

        ingredient.Name = request.Name.Trim();
        ingredient.Description = request.Description?.Trim();

        await _ingredientRepository.SaveChangesAsync();

        await _auditService.LogAsync(
            nameof(Ingredient),
            ingredient.Id.ToString(),
            ingredient.Name,
            AuditAction.Updated,
            oldValues,
            new { ingredient.Name, ingredient.Description },
            "Ingredient updated");

        _logger.LogInformation("Ingredient {IngredientId} updated", ingredient.Id);
        return _mapper.Map<IngredientResponseDto>(ingredient);
    }

    public async Task DeleteIngredient(int id)
    {
        var ingredient = await _ingredientRepository.Get(id)
            ?? throw new IngredientNotFoundException();

        // Prevent deleting an ingredient that is still referenced by active menu items
        var usageCount = ingredient.MenuItemIngredients?.Count ?? 0;
        if (usageCount > 0)
            throw new InvalidOperationException(
                $"Cannot delete ingredient '{ingredient.Name}' because it is used by {usageCount} menu item(s). " +
                "Remove it from those menu items first.");

        ingredient.IsDeleted = true;
        await _ingredientRepository.SaveChangesAsync();

        await _auditService.LogAsync(
            nameof(Ingredient),
            ingredient.Id.ToString(),
            ingredient.Name,
            AuditAction.Deleted,
            new { ingredient.IsDeleted },
            new { IsDeleted = true },
            "Ingredient soft-deleted");

        _logger.LogInformation("Ingredient {IngredientId} deleted", ingredient.Id);
    }

    // ── Menu-item ingredients ────────────────────────────────────────────────

    public async Task<ICollection<MenuItemIngredientResponseDto>> GetMenuItemIngredients(int menuItemId)
    {
        await EnsureMenuItemExists(menuItemId);
        var items = await _menuItemIngredientRepository.GetByMenuItemId(menuItemId);
        return MapIngredients(items);
    }

    public async Task<ICollection<MenuItemIngredientResponseDto>> SetMenuItemIngredients(
        int menuItemId, SetMenuItemIngredientsDto request)
    {
        await EnsureMenuItemExists(menuItemId);

        // Business rule: max 30 ingredients per menu item
        if (request.Ingredients.Count > 30)
            throw new InvalidOperationException("A menu item cannot have more than 30 ingredients.");

        ValidateIngredientEntries(request.Ingredients);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Full replace — delete existing rows first
            await _menuItemIngredientRepository.DeleteAllForMenuItem(menuItemId);
            await _menuItemIngredientRepository.SaveChangesAsync();

            var result = new List<MenuItemIngredient>();

            foreach (var dto in request.Ingredients)
            {
                int ingredientId;

                if (dto.NewIngredient != null)
                {
                    // Inline create: reuse if name already exists
                    ValidateIngredientName(dto.NewIngredient.Name);
                    var existing = await _ingredientRepository.GetByName(dto.NewIngredient.Name.Trim());

                    if (existing != null)
                    {
                        ingredientId = existing.Id;
                    }
                    else
                    {
                        var newIng = new Ingredient
                        {
                            Name = dto.NewIngredient.Name.Trim(),
                            Description = dto.NewIngredient.Description?.Trim(),
                            CreatedAt = DateTime.Now                        };
                        await _ingredientRepository.Create(newIng);
                        await _ingredientRepository.SaveChangesAsync();
                        ingredientId = newIng.Id;
                    }
                }
                else
                {
                    // Reference an existing ingredient
                    var ing = await _ingredientRepository.Get(dto.IngredientId!.Value)
                        ?? throw new IngredientNotFoundException(
                            $"Ingredient with ID {dto.IngredientId} was not found.");
                    ingredientId = ing.Id;
                }

                var mii = new MenuItemIngredient
                {
                    MenuItemId = menuItemId,
                    IngredientId = ingredientId,
                    ApproxQuantity = dto.ApproxQuantity,
                    Unit = dto.Unit?.Trim()
                };
                await _menuItemIngredientRepository.Create(mii);
                result.Add(mii);
            }

            await _menuItemIngredientRepository.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Set {Count} ingredients on menu item {MenuItemId}", result.Count, menuItemId);

            // Reload with navigation properties for response mapping
            var saved = await _menuItemIngredientRepository.GetByMenuItemId(menuItemId);
            return MapIngredients(saved);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ── Menu-item nutrition ──────────────────────────────────────────────────

    public async Task<MenuItemNutritionResponseDto?> GetMenuItemNutrition(int menuItemId)
    {
        await EnsureMenuItemExists(menuItemId);
        var nutrition = await _menuItemNutritionRepository.GetByMenuItemId(menuItemId);
        return nutrition == null ? null : _mapper.Map<MenuItemNutritionResponseDto>(nutrition);
    }

    public async Task<MenuItemNutritionResponseDto> SetMenuItemNutrition(
        int menuItemId, MenuItemNutritionDto request)
    {
        await EnsureMenuItemExists(menuItemId);
        ValidateNutrition(request);

        var existing = await _menuItemNutritionRepository.GetByMenuItemId(menuItemId);

        if (existing == null)
        {
            // Insert
            var nutrition = new MenuItemNutrition
            {
                MenuItemId = menuItemId,
                Calories = request.Calories,
                Protein = request.Protein,
                Carbohydrates = request.Carbohydrates,
                Fat = request.Fat,
                Fiber = request.Fiber,
                Sugar = request.Sugar,
                Sodium = request.Sodium
            };
            await _menuItemNutritionRepository.Create(nutrition);
            await _menuItemNutritionRepository.SaveChangesAsync();

            _logger.LogInformation("Nutrition created for menu item {MenuItemId}", menuItemId);
            return _mapper.Map<MenuItemNutritionResponseDto>(nutrition);
        }
        else
        {
            // Update
            var oldValues = new
            {
                existing.Calories, existing.Protein, existing.Carbohydrates,
                existing.Fat, existing.Fiber, existing.Sugar, existing.Sodium
            };

            existing.Calories = request.Calories;
            existing.Protein = request.Protein;
            existing.Carbohydrates = request.Carbohydrates;
            existing.Fat = request.Fat;
            existing.Fiber = request.Fiber;
            existing.Sugar = request.Sugar;
            existing.Sodium = request.Sodium;

            await _menuItemNutritionRepository.SaveChangesAsync();

            await _auditService.LogAsync(
                nameof(MenuItemNutrition),
                existing.Id.ToString(),
                $"MenuItem#{menuItemId}",
                AuditAction.Updated,
                oldValues,
                new
                {
                    existing.Calories, existing.Protein, existing.Carbohydrates,
                    existing.Fat, existing.Fiber, existing.Sugar, existing.Sodium
                },
                "Nutrition updated");

            _logger.LogInformation("Nutrition updated for menu item {MenuItemId}", menuItemId);
            return _mapper.Map<MenuItemNutritionResponseDto>(existing);
        }
    }

    public async Task DeleteMenuItemNutrition(int menuItemId)
    {
        await EnsureMenuItemExists(menuItemId);

        var existing = await _menuItemNutritionRepository.GetByMenuItemId(menuItemId)
            ?? throw new NutritionNotFoundException();

        _context.MenuItemNutritions.Remove(existing);
        await _menuItemNutritionRepository.SaveChangesAsync();

        _logger.LogInformation("Nutrition removed for menu item {MenuItemId}", menuItemId);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task EnsureMenuItemExists(int menuItemId)
    {
        var item = await _menuItemRepository.Get(menuItemId)
            ?? throw new MenuItemNotFoundException();
        _ = item; // suppress unused-variable warning
    }

    private static void ValidateIngredientName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Ingredient name is required.");

        if (name.Trim().Length > 100)
            throw new ArgumentException("Ingredient name cannot exceed 100 characters.");
    }

    private static void ValidateIngredientEntries(List<MenuItemIngredientDto> entries)
    {
        // Each entry must supply either IngredientId or NewIngredient, not both, not neither
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            bool hasId = entry.IngredientId.HasValue;
            bool hasNew = entry.NewIngredient != null;

            if (!hasId && !hasNew)
                throw new ArgumentException(
                    $"Ingredient entry at index {i} must provide either IngredientId or NewIngredient.");

            if (hasId && hasNew)
                throw new ArgumentException(
                    $"Ingredient entry at index {i} cannot provide both IngredientId and NewIngredient.");

            if (entry.ApproxQuantity.HasValue && entry.ApproxQuantity.Value <= 0)
                throw new ArgumentException(
                    $"Ingredient entry at index {i}: ApproxQuantity must be greater than zero.");

            if (!string.IsNullOrEmpty(entry.Unit) && entry.Unit.Length > 20)
                throw new ArgumentException(
                    $"Ingredient entry at index {i}: Unit cannot exceed 20 characters.");
        }

        // No duplicate IngredientIds within the same list
        var ids = entries.Where(e => e.IngredientId.HasValue).Select(e => e.IngredientId!.Value).ToList();
        if (ids.Count != ids.Distinct().Count())
            throw new ArgumentException("Duplicate ingredient IDs found in the request.");
    }

    private static void ValidateNutrition(MenuItemNutritionDto dto)
    {
        // Every provided value must be >= 0
        void Check(decimal? value, string field)
        {
            if (value.HasValue && value.Value < 0)
                throw new ArgumentException($"{field} cannot be negative.");
        }

        Check(dto.Calories, nameof(dto.Calories));
        Check(dto.Protein, nameof(dto.Protein));
        Check(dto.Carbohydrates, nameof(dto.Carbohydrates));
        Check(dto.Fat, nameof(dto.Fat));
        Check(dto.Fiber, nameof(dto.Fiber));
        Check(dto.Sugar, nameof(dto.Sugar));
        Check(dto.Sodium, nameof(dto.Sodium));
    }

    private static ICollection<MenuItemIngredientResponseDto> MapIngredients(
        ICollection<MenuItemIngredient> items)
    {
        return items.Select(mi => new MenuItemIngredientResponseDto
        {
            Id = mi.Id,
            IngredientId = mi.IngredientId,
            IngredientName = mi.Ingredient?.Name ?? string.Empty,
            ApproxQuantity = mi.ApproxQuantity,
            Unit = mi.Unit
        }).ToList();
    }
}
