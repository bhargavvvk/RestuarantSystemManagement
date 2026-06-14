using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class AdminWaiterController : ControllerBase
{
    private readonly IUserService _userService;
     private readonly IAdminService _adminService;
    public AdminWaiterController(IUserService userService,IAdminService adminService)
    {
        _userService = userService;
         _adminService=adminService;
    }
    [HttpPost("waiters")]
    public async Task<ActionResult<WaiterResponseDto>> AddWaiter(CreateWaiterRequestDto request)
    {
        var result =await _userService.CreateWaiterAsync(request);

        return CreatedAtAction(nameof(AddWaiter),new { id = result.Id },result);
    }
    [HttpGet("waiters")]
    public async Task<ActionResult<WaiterManagementResponseDto>>GetWaiters([FromQuery] string? search,[FromQuery] bool? isActive)
    {
        var result =await _adminService.GetWaiters(search,isActive);
        return Ok(result);
    }
    [HttpPatch("waiters/{waiterId}/status")]
    public async Task<IActionResult> UpdateWaiterStatus(int waiterId,[FromBody] UpdateWaiterStatusDto request)
    {
        await _adminService.UpdateWaiterStatus(waiterId, request.IsActive);
        return NoContent();
    }

    [HttpDelete("waiters/{waiterId}")]
    public async Task<IActionResult> DeleteWaiter(int waiterId)
    {
        await _adminService.DeleteWaiter(waiterId);
        return NoContent();
    }
}
