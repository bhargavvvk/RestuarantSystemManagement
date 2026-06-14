using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Controllers;

[ApiController]
[Route("api/admin")]
 [Authorize(Roles = "Admin")]
public class AdminTableController : ControllerBase
{
    private readonly ITableService _tableService;
    private readonly IAdminService _adminService;
    public AdminTableController(ITableService tableService,IAdminService adminService)
    {
        _tableService = tableService;
        _adminService=adminService;
    }

    [HttpGet("tables/dashboard")]
    public async Task<ActionResult<TableDashboardResponseDto>> GetTableDashboard()
    {
        var result = await _tableService.GetTableDashboard();
        return Ok(result);
    }
    [HttpPatch("tables/{tableId}/availability")]
    public async Task<ActionResult<RestaurantTable>>UpdateTableAvailability(int tableId,
            [FromBody] UpdateTableAvailabilityRequestDto request)
    {
        var table = await _tableService.UpdateTableAvailability(tableId,request.Status);
        return Ok(table);
    }
    [HttpPost("tables")]
    public async Task<ActionResult<TableResponseDto>>AddTable([FromBody] AddTableRequestDto request)
    {
        var table =await _tableService.AddTable(request);
        return CreatedAtAction(nameof(AddTable),new { id = table.Id },table);
    }
    [HttpPatch("tables/{tableId}/assign-waiter")]
    public async Task<ActionResult<TableResponseDto>>AssignWaiter(int tableId,[FromBody] AssignWaiterRequestDto request)
    {
        var result =await _adminService.AssignWaiter(tableId,request.WaiterId);
        return Ok(result);
    }
    [HttpPatch("tables/{tableId}/remove-waiter")]
    public async Task<ActionResult<TableResponseDto>>RemoveWaiter(int tableId)
    {
        var result =await _adminService.RemoveWaiter(tableId);
        return Ok(result);
    }
    [HttpGet("tables/{tableId}")]
    public async Task<ActionResult<TableDetailsDto>>GetTableDetails(int tableId)
    {
        var result =await _tableService.GetTableDetails(tableId);
        return Ok(result);
    }
    [HttpGet("tables/{tableId}/orders")]
    public async Task<ActionResult<ICollection<OrderResponseDto>>>GetTableOrders(int tableId)
    {
        var result =await _adminService.GetTableOrders(tableId);
        return Ok(result);
    }
    [HttpGet("tables/{tableId}/bill")]
    public async Task<ActionResult<BillResponseDto>>GetTableBill(int tableId)
    {
        var result =await _adminService.GetTableBill(tableId);
        return Ok(result);
    }
    [HttpPatch("tables/{tableId}/bill/service-charge")]
    public async Task<ActionResult<BillResponseDto>>UpdateServiceCharge(int tableId,UpdateServiceChargeRequestDto request)
    {
        var result =await _adminService.UpdateServiceCharge(tableId,request.IncludeServiceCharge);
        return Ok(result);
    }
    [HttpPatch("tables/{tableId}/orders/{orderId}/cancel")]
    public async Task<IActionResult> CancelOrder(int tableId,int orderId)
    {
        await _adminService.CancelOrder(tableId,orderId);
        return NoContent();
    }

    [HttpPatch(
    "tables/{tableId}/orders/{orderId}/items/{orderItemId}/cancel")]
    public async Task<IActionResult>CancelOrderItem(int tableId,int orderId,int orderItemId)
    {
        await _adminService.CancelOrderItem(tableId,orderId,orderItemId);
        return NoContent();
    }
    [HttpPatch(
    "tables/{tableId}/orders/{orderId}/items/{orderItemId}/quantity")]
    public async Task<IActionResult>UpdateOrderItemQuantity(int tableId,int orderId,int orderItemId,[FromBody] UpdateOrderItemQuantityDto request)
    {
        await _adminService.UpdateOrderItemQuantity(tableId,orderId,orderItemId,request.Quantity);
        return NoContent();
    }
    [HttpDelete("tables/{tableId}")]
    public async Task<IActionResult> DeleteTable(int tableId)
    {
        await _tableService.DeleteTable(tableId);
        return NoContent();
    }
}
