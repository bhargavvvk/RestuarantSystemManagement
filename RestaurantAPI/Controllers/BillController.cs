using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BillController : ControllerBase
{
    private readonly IBillService _billService;
    public BillController(IBillService billService)
    {
       _billService=billService;
    }
    [Authorize(Roles = "Customer")]

    [HttpGet("Customer")]
    public async Task<ActionResult<BillResponseDto>> GetBill()
    {
        var sessionId =int.Parse(User.FindFirst("SessionId")!.Value);
        var bill =await _billService.GetBill(sessionId);
        return Ok(bill);
    }
    [HttpGet("payment-methods")]
    public ActionResult<ICollection<LookupDto>>GetPaymentMethods()
    {
        return Ok(_billService.GetPaymentMethods());
    }
}
