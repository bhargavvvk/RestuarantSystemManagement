using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;
using RestaurantAPI.Services;

namespace RestaurantAPI.Controllers;

[ApiController]
[Authorize(Roles = "Waiter")]
[Route("api/[controller]")]
public class WaiterController : ControllerBase
{
    private readonly ITableService _tableService;
    private readonly IWaiterService _waiterService;
    private readonly IIOrderService _orderService;
    private readonly ICustomerRequestService _customerRequestService;
    private readonly IDiningSessionService _diningSessionService;
    public WaiterController(ITableService tableService, IWaiterService waiterService, IIOrderService orderService,
     ICustomerRequestService customerRequestService,IDiningSessionService diningSessionService)
    {
        _tableService = tableService;
        _waiterService = waiterService;
        _orderService = orderService;
        _customerRequestService = customerRequestService;
        _diningSessionService = diningSessionService;
    }

    [HttpGet("tables")]
    public async Task<ActionResult<IEnumerable<WaiterTableResponseDto>>>GetAssignedTables()
    {
        var waiterId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result =await _tableService.GetAssignedTablesAsync(waiterId);

        return Ok(result);
    }

    [HttpGet("tables/{tableId}/cart")]
    public async Task<ActionResult<ICollection<CartItemResponseDto>>>
        GetTableCart(int tableId)
    {
        var waiterId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _waiterService.GetTableCart(waiterId, tableId);
        return Ok(result);
    }

    [HttpPost("tables/{tableId}/cart/items")]
    public async Task<ActionResult<CartItemResponseDto>>AddItemToCart(int tableId,AddToCartDto request)
    {
        var waiterId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await _waiterService.AddItemToTableCart(waiterId,tableId,request);
        return Ok();
    }

    [HttpPut("tables/{tableId}/cart/items/{cartItemId}")]
    public async Task<ActionResult<CartItemResponseDto>>UpdateCartItem(int tableId,int cartItemId,UpdateCartItemDto request)
    {
        var waiterId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

         await _waiterService.UpdateTableCartItem(waiterId,tableId,cartItemId,request);
        return Ok();
    }

    [HttpDelete("tables/{tableId}/cart/items/{cartItemId}")]
    public async Task<IActionResult> RemoveCartItem(int tableId,int cartItemId)
    {
        var waiterId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await _waiterService.RemoveTableCartItem(waiterId,tableId,cartItemId);
        return NoContent();
    }

    [HttpGet("tables/{tableId}/orders")]
    public async Task<ActionResult<ICollection<OrderResponseDto>>>GetTableOrders(int tableId)
    {
        var waiterId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _waiterService.GetTableOrders(waiterId, tableId);
        return Ok(result);
    }

    [HttpPost("tables/{tableId}/orders")]
    public async Task<ActionResult<string>> PlaceOrder(int tableId,PlaceOrderRequestDto request)
    {
        var waiterId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await _waiterService.PlaceOrder(waiterId, tableId, request);
        return Ok("Order Placed Successfull");
    }

    [HttpPut("tables/{tableId}/orders/items/{orderItemId}/serve")]
    public async Task<ActionResult<OrderItemResponseDto>>MarkAsServed(int tableId,int orderItemId)
    {
        var waiterId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _waiterService.MarkOrderItemAsServed(waiterId,tableId,orderItemId);
        return Ok(result);
    }

    [HttpGet("tables/{tableId}/bill")]
    public async Task<ActionResult<BillResponseDto>>GetTableBill(int tableId)
    {
        var waiterId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _waiterService.GetTableBill(waiterId, tableId);
        return Ok(result);
    }

    [HttpPut("tables/{tableId}/bill/pay")]
    public async Task<ActionResult<BillResponseDto>>MarkBillPaid(int tableId,MarkBillPaidDto request)
    {
        var waiterId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _waiterService.MarkTableBillAsPaid(waiterId,tableId,request);
        return Ok(result);
    }

    [HttpPut("tables/{tableId}/close-session")]
    public async Task<IActionResult> CloseSession(int tableId)
    {
        var waiterId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await _diningSessionService.CloseSession(waiterId,tableId);
        return NoContent();
    }
    

    [HttpGet("requests")]
    public async Task<ActionResult<ICollection<CustomerRequestResponseDto>>>GetRequests()
    {
        var waiterId =int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var requests =await _customerRequestService.GetActiveRequests(waiterId);
        return Ok(requests);
    }

    
    [HttpPatch("requests/{requestId}/complete")]
    public async Task<ActionResult>CompleteRequest(int requestId)
    {
        var waiterId =int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await _customerRequestService.CompleteRequest(waiterId,requestId);
        return Ok();
    }
}
