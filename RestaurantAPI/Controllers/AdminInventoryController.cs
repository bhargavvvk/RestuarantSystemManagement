using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
 [Authorize(Roles = "Admin")]
public class AdminInventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    public AdminInventoryController( IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }
    [HttpGet("inventory")]
    public async Task<ActionResult<InventoryManagementResponseDto>>GetInventoryItems(
            [FromQuery] string? search,
            [FromQuery] bool lowStockOnly = false,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
    {
        var result =await _inventoryService.GetInventoryItems(
                search,
                lowStockOnly,
                pageNumber,
                pageSize);

        return Ok(result);
    }
    
    [HttpPatch("inventory/{inventoryItemId}/quantity")]
    public async Task<IActionResult>UpdateInventoryQuantity(
            int inventoryItemId,
            [FromBody]
            UpdateInventoryQuantityDto request)
    {
        await _inventoryService.UpdateInventoryQuantity(inventoryItemId,request.Quantity);
        return NoContent();
    }

    [HttpPatch("inventory/{inventoryItemId}/threshold")]
    public async Task<IActionResult>UpdateInventoryThreshold(int inventoryItemId,[FromBody] UpdateInventoryThresholdDto request)
    {
        await _inventoryService.UpdateInventoryThreshold(inventoryItemId,request.ThresholdQuantity);
        return NoContent();
    }

    [HttpPost("inventory")]
    public async Task<IActionResult>AddInventoryItem([FromBody]AddInventoryItemDto request)
    {
        await _inventoryService.AddInventoryItem(request);
        return NoContent();
    }

    [HttpDelete("inventory/{inventoryItemId}")]
    public async Task<IActionResult>DeleteInventoryItem(int inventoryItemId)
    {
        await _inventoryService.DeleteInventoryItem(inventoryItemId);
        return NoContent();
    }
}
