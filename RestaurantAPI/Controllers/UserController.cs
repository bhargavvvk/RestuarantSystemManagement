using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IDiningSessionService _diningSessionService;
    public UserController(IUserService userService,IDiningSessionService diningSessionService)
    {
        _userService = userService;
        _diningSessionService = diningSessionService;
    }
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto request)
    {
        var result =await _userService.LoginAsync(request);
        return Ok(result);
    }
    [HttpPost("system-users")]
    public async Task<ActionResult<SystemUsersCreatedResponseDto>>CreateSystemUsers(CreateSystemUsersRequestDto request)
    {
        var result =
            await _userService.CreateSystemUsersAsync(request);

        return Ok(result);
    }
    [Authorize(Roles ="Admin,Waiter,Kitchen")]
    [HttpGet("profile")]
    public async Task<ActionResult<ProfileResponseDto>>GetProfile()
    {
        var result =await _userService.GetProfile();
        return Ok(result);
    }
    [Authorize(Roles ="Admin,Waiter,Kitchen")]
    [HttpPut]
    public async Task<IActionResult>UpdateProfile(UpdateProfileDto request)
    {
        await _userService.UpdateProfile(request);
        return NoContent();
    }
    [HttpPut("password")]
    public async Task<IActionResult>ChangePassword(ChangePasswordDto request)
    {
        await _userService.ChangePassword(request);
        return NoContent();
    }
    [Authorize(Roles = "Customer")]
    [HttpGet("session/validate")]
    public async Task<ActionResult<SessionValidationResponseDto>>ValidateSession()
    {
        var sessionId = int.Parse(User.FindFirst("SessionId")!.Value);
        return Ok(await _diningSessionService.ValidateSession(sessionId));
    }
}
