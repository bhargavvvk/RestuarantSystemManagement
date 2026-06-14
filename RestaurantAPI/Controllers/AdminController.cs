using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IIOrderService _orderService;
    private readonly IBillService _billService;
    private readonly IMenuService _menuService;
    private readonly ITaxConfigurationService _taxConfigurationService;
    public AdminController(IUserService userService,IIOrderService orderService,IBillService billService,IMenuService menuService,IAdminService adminService,ITaxConfigurationService taxConfigurationService)
    {
        _userService = userService;
        _orderService = orderService;
        _billService = billService;
        _menuService = menuService;
        _taxConfigurationService = taxConfigurationService;
    }
    [HttpGet("orders")]
    public async Task<ActionResult<PagedResponseDto<OrderRegistryDto>>> GetOrders(
            [FromQuery] string? search,
            [FromQuery] DateOnly? date,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
    {
        var result =
        await _orderService.GetAllOrders(search,date,pageNumber,pageSize);
        return Ok(result);
    }
    [HttpGet("orders/{orderId}")]
    public async Task<ActionResult<OrderDetailsDto>>GetOrderDetails(int orderId)
    {
        var result =await _orderService.GetOrderDetails(orderId);
        return Ok(result);
    }
    [HttpGet("bills")]
    public async Task<ActionResult<PagedResponseDto<BillRegistryDto>>> GetBills(
            [FromQuery] string? search,
            [FromQuery] DateOnly? date,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
    {
        var result =await _billService.GetBills(search,date,pageNumber,pageSize);

        return Ok(result);
    }
    [HttpGet("bills/summary")]
    public async Task<ActionResult<BillDashboardSummaryDto>>GetBillDashboardSummary([FromQuery] DateOnly? date)
    {
        var result =await _billService.GetBillDashboardSummary(date);

        return Ok(result);
    }
    [HttpGet("bills/{billId}")]
    public async Task<ActionResult<BillDetailsDto>>GetBillDetails(int billId)
    {
        var result =
            await _billService
                .GetBillDetails(billId);

        return Ok(result);
    }
    [HttpPatch("menu/{menuItemId}/availability")]
    public async Task<IActionResult> ToggleMenuAvailability(
    int menuItemId,
    [FromBody] UpdateAvailabilityDto request)
    {
        await _menuService.ToggleMenuAvailability(menuItemId,request.IsAvailable);
        return NoContent();
    }
    [HttpPatch("categories/{categoryId}/availability")]
    public async Task<IActionResult> ToggleCategoryAvailability(int categoryId, [FromBody] UpdateAvailabilityDto request)
    {
        await _menuService.ToggleCategoryAvailability(categoryId,request.IsAvailable);
        return NoContent();
    }
    [HttpPost("menu")]
    public async Task<ActionResult<MenuItemResponseDto>>AddMenuItem([FromForm] AddMenuItemDto request)
    {
        var result =await _menuService.AddMenuItem(request);

        return CreatedAtAction(nameof(AddMenuItem),new { id = result.Id },result);
    }
    [HttpPost("categories")]
    public async Task<ActionResult<CategoryResponseDto>>AddCategory([FromBody]AddCategoryDto request)
    {
        var result =await _menuService.AddCategory(request);
        return CreatedAtAction(nameof(AddCategory),new { id = result.Id },result);
    }
    [HttpPut("menu/{menuItemId}")]
    public async Task<ActionResult<MenuItemResponseDto>>UpdateMenuItem(int menuItemId,[FromBody] UpdateMenuItemDto request)
    {
        var result =await _menuService.UpdateMenuItem(menuItemId,request);
        return Ok(result);
    }
    [HttpPut("categories/{categoryId}")]
    public async Task<ActionResult<CategoryResponseDto>>UpdateCategory(int categoryId,[FromBody] UpdateCategoryDto request)
    {
        var result =await _menuService.UpdateCategory(categoryId,request);

        return Ok(result);
    }
    [HttpDelete("menu/{menuItemId}")]
    public async Task<IActionResult>DeleteMenuItem(int menuItemId)
    {
        await _menuService.DeleteMenuItem(menuItemId);
        return NoContent();
    }
    [HttpDelete("categories/{categoryId}")]
    public async Task<IActionResult>DeleteCategory(int categoryId)
    {
        await _menuService.DeleteCategory(categoryId);
        return NoContent();
    }
    [HttpGet("tax-configuration")]
    public async Task<ActionResult<TaxConfigurationResponseDto>>GetTaxConfiguration()
    {
        var result =await _taxConfigurationService.GetTaxConfiguration();
        return Ok(result);
    }
    [HttpPut("tax-configuration")]
    public async Task<IActionResult>UpdateTaxConfiguration([FromBody]UpdateTaxConfigurationDto request)
    {
        await _taxConfigurationService.UpdateTaxConfiguration(request);
        return NoContent();
    }
}
