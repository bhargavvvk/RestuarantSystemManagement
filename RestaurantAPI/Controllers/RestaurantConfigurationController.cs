using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Controllers;

[ApiController]
[Route("api/restaurant")]
public class RestaurantConfigurationController : ControllerBase
{
    private readonly IRestaurantConfigurationService _service;

    public RestaurantConfigurationController(IRestaurantConfigurationService service)
    {
        _service = service;
    }

    // ── Public read ──────────────────────────────────────────────────────────

    /// <summary>
    /// GET api/restaurant
    /// Returns all restaurant details including the parsed KnowledgeBase.
    /// Public — used by the frontend home/about page.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<RestaurantConfigurationResponseDto>> GetConfiguration()
    {
        var result = await _service.GetConfiguration();
        return Ok(result);
    }

    /// <summary>
    /// GET api/restaurant/knowledge-base
    /// Returns only the KnowledgeBase JSON as a parsed object.
    /// Returns 204 if no knowledge base has been set.
    /// Public — useful for chatbot / FAQ rendering.
    /// </summary>
    [HttpGet("knowledge-base")]
    public async Task<IActionResult> GetKnowledgeBase()
    {
        var result = await _service.GetKnowledgeBase();
        if (result == null)
            return NoContent();

        return Ok(result);
    }

    // ── Admin writes ─────────────────────────────────────────────────────────

    /// <summary>
    /// PUT api/restaurant/details
    /// Creates or updates the restaurant's basic details (name, address, hours, etc.).
    /// Does NOT touch the KnowledgeBase — use the dedicated endpoint for that.
    /// </summary>
    [HttpPut("details")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RestaurantConfigurationResponseDto>> UpsertDetails(
        [FromBody] UpdateRestaurantDetailsDto request)
    {
        var result = await _service.UpsertDetails(request);
        return Ok(result);
    }

    /// <summary>
    /// PUT api/restaurant/knowledge-base
    /// Full replace of the KnowledgeBase JSON.
    /// Must be a JSON object or array. Send { "value": null } to clear.
    /// </summary>
    [HttpPut("knowledge-base")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RestaurantConfigurationResponseDto>> UpdateKnowledgeBase(
        [FromBody] UpdateKnowledgeBaseDto request)
    {
        var result = await _service.UpdateKnowledgeBase(request);
        return Ok(result);
    }
}
