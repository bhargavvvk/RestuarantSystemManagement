using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;
using RestaurantAPI.Exceptions;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Services;

public class InventoryService:IInventoryService
{
    private readonly IInventoryItemRepository _inventoryRepository;
    private readonly RestaurantContext _context;
    private readonly IAuditService _auditService;
    public InventoryService(IInventoryItemRepository inventoryRepository,RestaurantContext context,IAuditService auditService)
    {
        _inventoryRepository =inventoryRepository;
        _context = context;
        _auditService=auditService;
    }
    public async Task<InventoryManagementResponseDto>GetInventoryItems(string? search,bool lowStockOnly,
            int pageNumber,int pageSize)
    {
        var query =_inventoryRepository.GetInventoryQuery();
        var summary =new InventorySummaryDto
            {
                TotalItems =await query.CountAsync(),
                LowStockItems =await query.CountAsync(i =>i.AvailableQuantity <i.MinimumStockThreshold)
            };
            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch =search.Trim().ToUpper();
                query = query.Where(i =>i.Name.ToUpper().Contains(normalizedSearch));
            }
        if (lowStockOnly)
        {
            query = query.Where(i =>i.AvailableQuantity <i.MinimumStockThreshold);
        }

        var totalCount =await query.CountAsync();
        var items =
            await query
                .OrderBy(i => i.Name)
                .Skip((pageNumber - 1)* pageSize)
                .Take(pageSize)
                .Select(i =>
                    new InventoryItemCardDto
                    {
                        Id = i.Id,
                        ItemName =i.Name,
                        CurrentQuantity =i.AvailableQuantity,
                        ThresholdQuantity =i.MinimumStockThreshold,
                        Unit =i.Unit,
                        UpdatedAt =i.LastUpdatedAt
                    })
                .ToListAsync();
        return new InventoryManagementResponseDto
        {
            Summary = summary,
            Items =
                new PagedResponseDto<InventoryItemCardDto>
                {
                    PageNumber =pageNumber,
                    PageSize =pageSize,
                    TotalCount =totalCount,
                    Items =items
                }
        };
    }
    public async Task UpdateInventoryQuantity(int inventoryItemId,decimal quantity)
    {
        if (quantity < 0)
        {
            throw new Exception("Quantity cannot be negative");
        }
        var inventoryItem =await _inventoryRepository.Get(inventoryItemId);
        if (inventoryItem == null)
        {
            throw new InventoryItemNotFoundException("Item not found");
        }
        var oldValues = new
        {
            inventoryItem.AvailableQuantity

        };
        await UpdateInventoryField(
            inventoryItemId,
            item => item.AvailableQuantity = quantity,
            oldValues,
            new
            {
                CurrentQuantity = quantity
            },
            "Inventory quantity updated");
    }
    public async Task UpdateInventoryThreshold(int inventoryItemId,decimal thresholdQuantity)
    {
        if (thresholdQuantity < 0)
        {
            throw new Exception("Threshold quantity cannot be negative");
        }
        var inventoryItem =await _inventoryRepository.Get(inventoryItemId);
        if (inventoryItem == null)
        {
            throw new InventoryItemNotFoundException("Item not found");
        }

        var oldValues = new
        {
            inventoryItem.MinimumStockThreshold
        };

        await UpdateInventoryField(
            inventoryItemId,
            item => item.MinimumStockThreshold =thresholdQuantity,
            oldValues,
            new
            {
                ThresholdQuantity =
                    thresholdQuantity
            },
            "Inventory threshold updated");
    }
    private async Task UpdateInventoryField(int inventoryItemId,Action<InventoryItem> updateAction,object oldValues,
        object newValues,string remarks)
    {
        var inventoryItem =await _inventoryRepository.Get(inventoryItemId);
        if (inventoryItem == null)
        {
            throw new InventoryItemNotFoundException("item not found");
        }

        await using var transaction =await _context.Database.BeginTransactionAsync();
        try
        {
            updateAction(inventoryItem);
            inventoryItem.LastUpdatedAt =DateTime.Now;
            await _inventoryRepository.SaveChangesAsync();
            await _auditService.LogAsync(nameof(InventoryItem),inventoryItem.Id.ToString(),
                AuditAction.Updated,
                oldValues,
                newValues,
                remarks);

            await _inventoryRepository.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    public async Task AddInventoryItem(AddInventoryItemDto request)
    {
        if(string.IsNullOrWhiteSpace(request.ItemName))
        {
            throw new Exception("Item name is required");
        }

        if(string.IsNullOrWhiteSpace(request.Unit))
        {
            throw new Exception("Unit is required");
        }
        if(request.CurrentQuantity < 0)
        {
            throw new Exception("Quantity cannot be negative");
        }

        if(request.ThresholdQuantity < 0)
        {
            throw new Exception("Threshold quantity cannot be negative");
        }
        var existing =await _inventoryRepository.GetByName(request.ItemName.Trim());
        if(existing != null)
        {
            throw new DuplicateEntityException("Inventory item already exists");
        }
        var item =new InventoryItem
            {
                Name =request.ItemName.Trim(),
                AvailableQuantity =request.CurrentQuantity,
                MinimumStockThreshold=request.ThresholdQuantity,
                Unit =request.Unit.Trim(),
                LastUpdatedAt =DateTime.Now
            };

        await using var transaction =await _context.Database.BeginTransactionAsync();
        try
        {
            await _inventoryRepository.Create(item);
            await _inventoryRepository.SaveChangesAsync();
            await _auditService.LogAsync(
                nameof(InventoryItem),
                item.Id.ToString(),
                AuditAction.Created,
                null,
                new
                {
                    item.Name,
                    item.AvailableQuantity,
                    item.MinimumStockThreshold,
                    item.Unit
                },
                "Inventory item created");

            await _inventoryRepository.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    public async Task DeleteInventoryItem(int inventoryItemId)
    {
        var inventoryItem =await _inventoryRepository.Get(inventoryItemId);
        if(inventoryItem == null)
        {
            throw new InventoryItemNotFoundException("Item Not Found");
        }

        var oldValues = new
        {
            inventoryItem.IsDeleted
        };

        await using var transaction =await _context.Database.BeginTransactionAsync();

        try
        {
            inventoryItem.IsDeleted = true;

            await _inventoryRepository.SaveChangesAsync();

            await _auditService.LogAsync(
                nameof(InventoryItem),
                inventoryItem.Id.ToString(),
                AuditAction.Deleted,
                oldValues,
                new
                {
                    inventoryItem.IsDeleted
                },
                $"Inventory item {inventoryItem.Name} deleted");

            await _inventoryRepository.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
