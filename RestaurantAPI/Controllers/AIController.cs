using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantAPI.Services.AI.Contracts;
using RestaurantAPI.Services.AI.Contracts.Models;

namespace RestaurantAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Customer")]
public class AIController : ControllerBase
{
    private readonly IAIService _aiService;

    public AIController(IAIService aiService)
    {
        _aiService = aiService;
    }

    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponse>> Chat(ChatRequest request)
    {
        var response = await _aiService.ChatAsync(request);

        return Ok(response);
    }
}