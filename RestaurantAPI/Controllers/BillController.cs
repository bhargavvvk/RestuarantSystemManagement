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

    [Authorize(Roles = "Customer")]
    [HttpGet("Customer/split")]
    public async Task<ActionResult<SplitBillResponseDto>> GetSplitBill()
    {
        var sessionId = int.Parse(User.FindFirst("SessionId")!.Value);
        var split = await _billService.GetSplitBill(sessionId);
        return Ok(split);
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("Customer/my-table")]
    public async Task<ActionResult<TableBillDto>> GetMyTableBill()
    {
        var sessionId = int.Parse(User.FindFirst("SessionId")!.Value);
        var tableId = int.Parse(User.FindFirst("TableId")!.Value);
        var result = await _billService.GetTableBill(sessionId, tableId);
        return Ok(result);
    }

    [Authorize(Roles = "Customer")]
    [HttpPut("Customer/split")]
    public async Task<IActionResult> SaveCustomSplits([FromBody] SaveCustomSplitsDto request)
    {
        var sessionId = int.Parse(User.FindFirst("SessionId")!.Value);
        await _billService.SaveCustomSplits(sessionId, request.CustomSplitsJson);
        return Ok();
    }

    [HttpGet("payment-methods")]
    public ActionResult<ICollection<LookupDto>>GetPaymentMethods()
    {
        return Ok(_billService.GetPaymentMethods());
    }
}
