using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerOrderController : ControllerBase
{
    private readonly IIOrderService _orderService;
    public CustomerOrderController(IIOrderService orderService)
    {
        _orderService = orderService;
    }
    [Authorize(Roles = "Customer")]
    [HttpPost]
    public async Task<ActionResult>PlaceOrder(PlaceOrderRequestDto request)
    {
        var sessionId =int.Parse(User.FindFirst("SessionId")!.Value);
        await _orderService.PlaceOrder(sessionId,request);
        return Created();
    }
    [Authorize(Roles = "Customer")]
    [HttpGet]
    public async Task<ActionResult<ICollection<OrderResponseDto>>> GetOrders()
    {
        var sessionId =int.Parse(User.FindFirst("SessionId")!.Value);
        var orders =await _orderService.GetOrders(sessionId);
        return Ok(orders);
    }
}
