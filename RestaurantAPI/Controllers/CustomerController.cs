using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ICustomerRequestService _customerRequestService;
    private readonly IDiningSessionService _diningSessionService;
    private readonly ITableService _tableService;
    public CustomerController(IUserService userService,
    ICustomerRequestService customerRequestService, IDiningSessionService diningSessionService,ITableService tableService)
    {
        _userService = userService;
        _customerRequestService = customerRequestService;
        _diningSessionService = diningSessionService;
        _tableService=tableService;
    }
    [HttpGet("tables/{qrIdentifier}")]
    public async Task<ActionResult<TableStatusResponseDto>> GetTableStatus(string qrIdentifier)
    {
       
            var result = await _tableService.GetTableStatus(qrIdentifier);
            return Ok(result);
        
    }
   [HttpPost("tables/{qrIdentifier}/sessions")]
    public async Task<ActionResult<CreateSessionResponseDto>>CreateSession(string qrIdentifier,CreateSessionRequestDto request)

    {
            var result =await _diningSessionService.CreateSession(qrIdentifier,request);
            return Created("", result);

    }
    [HttpPost("tables/{qrIdentifier}/sessions/join")]
    public async Task<ActionResult<JoinSessionResponseDto>>JoinSession( string qrIdentifier,JoinSessionRequestDto request)
    {
        
            var result =await _diningSessionService.JoinSession(qrIdentifier,request);
            return Ok(result);
    }
   [Authorize(Roles = "Customer")]
    [HttpPost("requests")]
    public async Task<ActionResult> CreateRequest(CreateCustomerRequestDto request)
    {
            var sessionId = int.Parse(User.FindFirst("SessionId")!.Value);
            var result =await _customerRequestService.CreateRequest(sessionId,request);
            return Ok(result);
    }
}
