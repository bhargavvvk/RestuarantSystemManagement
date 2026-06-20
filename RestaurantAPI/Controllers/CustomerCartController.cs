using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize(Roles = "Customer")]
public class CustomerCartController : ControllerBase
{
    private readonly ICartService _cartService;
    public CustomerCartController(ICartService cartService)
    {
        _cartService = cartService;
    }
    [HttpGet]
    public async Task<ActionResult<ICollection<CartItemResponseDto>>> GetCart()
    {
        
            var cartId =int.Parse(User.FindFirst("CartId")!.Value);
            var cartItems =await _cartService.GetCartItems(cartId);
            return Ok(cartItems);
    }
    [HttpPost("items")]
    public async Task<ActionResult>AddToCart(AddToCartDto request)
    {

        var cartId = int.Parse(User.FindFirst("CartId")!.Value);
        var sessionId = int.Parse(User.FindFirst("SessionId")!.Value);
        await _cartService.AddToCart(sessionId,cartId,request);
            return Created();
    }
    [HttpPatch("items/{cartItemId}")]
    public async Task<ActionResult>UpdateCartItem(int cartItemId,UpdateCartItemDto request)
    {
        var cartId = int.Parse(User.FindFirst("CartId")!.Value);
        var sessionId = int.Parse(User.FindFirst("SessionId")!.Value);
        await _cartService.UpdateCartItem(sessionId,cartId, cartItemId, request);
        return NoContent();
    }
    [HttpDelete("items/{cartItemId}")]
    public async Task<ActionResult>RemoveCartItem(int cartItemId)
    {
        var cartId = int.Parse(User.FindFirst("CartId")!.Value);
        var sessionId = int.Parse(User.FindFirst("SessionId")!.Value);
        await _cartService.RemoveCartItem(sessionId,cartId,cartItemId);
        return NoContent();
    }
}
