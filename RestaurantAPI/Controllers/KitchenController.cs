using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Controllers;
[Authorize(Roles = "KitchenStaff")]
[ApiController]
[Route("api/[controller]")]
public class KitchenController : ControllerBase
{
    readonly IIOrderService _orderService;
    readonly IKitchenService _kitchenService;
    public KitchenController(IIOrderService orderService,IKitchenService kitchenService)
    {
        _orderService = orderService;
        _kitchenService = kitchenService;
    }
    [HttpGet]
    public async Task<ActionResult<KitchenOrdersResponseDto>>GetKitchenOrders([FromQuery] OrderItemStatus status)
    {
        var result =await _orderService.GetKitchenOrders(status);
        return Ok(result);
    }
    [HttpGet("today-order-count")]
    public async Task<ActionResult<int>> GetTodayOrderCount()
    {
        return Ok(await _orderService.GetTodayOrderCount());
    }

    [HttpPatch("orders/{orderId}/start-preparing")]
    public async Task<IActionResult> StartPreparing(int orderId)
    {
        await _kitchenService.StartPreparing(orderId);
        return NoContent();
    }

    [HttpPatch("orders/{orderId}/items/{orderItemId}/ready")]
    public async Task<IActionResult> MarkOrderItemReady(int orderId,int orderItemId)
    {
        await _kitchenService.MarkOrderItemReady(orderId,orderItemId);
        return NoContent();
    }
}
