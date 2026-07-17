using System.Text.Json;
using System.Text.Json.Nodes;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Services;

public class RestaurantConfigurationService : IRestaurantConfigurationService
{
    private readonly IRestaurantConfigurationRepository _repo;
    private readonly ILogger<RestaurantConfigurationService> _logger;

    public RestaurantConfigurationService(
        IRestaurantConfigurationRepository repo,
        ILogger<RestaurantConfigurationService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<RestaurantConfigurationResponseDto> GetConfiguration()
    {
        var config = await _repo.GetConfiguration();
        if (config == null)
            throw new InvalidOperationException(
                "Restaurant configuration has not been set up yet.");

        return ToDto(config);
    }

    public async Task<RestaurantConfigurationResponseDto> UpsertDetails(
        UpdateRestaurantDetailsDto request)
    {
        ValidateDetails(request);

        var config = await _repo.GetConfiguration();

        if (config == null)
        {
            // First-time setup — create the single row
            config = new RestaurantConfiguration
            {
                RestaurantName = request.RestaurantName.Trim(),
                Description = request.Description?.Trim(),
                Address = request.Address?.Trim(),
                PhoneNumber = request.PhoneNumber?.Trim(),
                Email = request.Email?.Trim(),
                OpeningTime = ParseTime(request.OpeningTime),
                ClosingTime = ParseTime(request.ClosingTime),
                About = request.About?.Trim(),
                LastUpdatedAt = DateTime.Now
            };
            await _repo.Create(config);
            _logger.LogInformation("Restaurant configuration created.");
        }
        else
        {
            config.RestaurantName = request.RestaurantName.Trim();
            config.Description = request.Description?.Trim();
            config.Address = request.Address?.Trim();
            config.PhoneNumber = request.PhoneNumber?.Trim();
            config.Email = request.Email?.Trim();
            config.OpeningTime = ParseTime(request.OpeningTime);
            config.ClosingTime = ParseTime(request.ClosingTime);
            config.About = request.About?.Trim();
            config.LastUpdatedAt = DateTime.Now;
            _logger.LogInformation("Restaurant configuration updated.");
        }

        await _repo.SaveChangesAsync();
        return ToDto(config);
    }

    public async Task<RestaurantConfigurationResponseDto> UpdateKnowledgeBase(
        UpdateKnowledgeBaseDto request)
    {
        var config = await _repo.GetConfiguration()
            ?? throw new InvalidOperationException(
                "Set up restaurant details first before updating the knowledge base.");

        // Validate JSON is an object or array (not a primitive)
        if (request.Value != null)
        {
            var kind = request.Value.GetValueKind();
            if (kind != JsonValueKind.Object && kind != JsonValueKind.Array)
                throw new ArgumentException(
                    "KnowledgeBase must be a JSON object or array, not a primitive value.");
        }

        config.KnowledgeBase = request.Value?.ToJsonString();
        config.LastUpdatedAt = DateTime.Now;

        await _repo.SaveChangesAsync();
        _logger.LogInformation("Restaurant knowledge base updated.");
        return ToDto(config);
    }

    public async Task<object?> GetKnowledgeBase()
    {
        var config = await _repo.GetConfiguration()
            ?? throw new InvalidOperationException(
                "Restaurant configuration has not been set up yet.");

        if (string.IsNullOrWhiteSpace(config.KnowledgeBase))
            return null;

        // Return as parsed object so it renders as proper JSON, not an escaped string
        return JsonNode.Parse(config.KnowledgeBase);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void ValidateDetails(UpdateRestaurantDetailsDto r)
    {
        if (string.IsNullOrWhiteSpace(r.RestaurantName))
            throw new ArgumentException("Restaurant name is required.");

        if (r.RestaurantName.Trim().Length > 100)
            throw new ArgumentException("Restaurant name cannot exceed 100 characters.");

        if (r.OpeningTime != null && !IsValidTimeString(r.OpeningTime))
            throw new ArgumentException("OpeningTime must be in HH:mm format (e.g. '09:00').");

        if (r.ClosingTime != null && !IsValidTimeString(r.ClosingTime))
            throw new ArgumentException("ClosingTime must be in HH:mm format (e.g. '22:30').");

        if (r.OpeningTime != null && r.ClosingTime != null)
        {
            var open = ParseTime(r.OpeningTime)!.Value;
            var close = ParseTime(r.ClosingTime)!.Value;
            if (close <= open)
                throw new ArgumentException("ClosingTime must be after OpeningTime.");
        }
    }

    private static bool IsValidTimeString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        return TimeSpan.TryParseExact(value.Trim(), @"hh\:mm", null, out _)
            || TimeSpan.TryParseExact(value.Trim(), @"h\:mm", null, out _);
    }

    private static TimeSpan? ParseTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        if (TimeSpan.TryParseExact(value.Trim(), @"hh\:mm", null, out var ts)) return ts;
        if (TimeSpan.TryParseExact(value.Trim(), @"h\:mm", null, out ts)) return ts;
        return null;
    }

    private static RestaurantConfigurationResponseDto ToDto(RestaurantConfiguration c)
    {
        JsonNode? kb = null;
        if (!string.IsNullOrWhiteSpace(c.KnowledgeBase))
        {
            try { kb = JsonNode.Parse(c.KnowledgeBase); }
            catch { /* malformed stored JSON — return null */ }
        }

        return new RestaurantConfigurationResponseDto
        {
            Id = c.Id,
            RestaurantName = c.RestaurantName,
            Description = c.Description,
            Address = c.Address,
            PhoneNumber = c.PhoneNumber,
            Email = c.Email,
            OpeningTime = c.OpeningTime.HasValue
                ? c.OpeningTime.Value.ToString(@"hh\:mm") : null,
            ClosingTime = c.ClosingTime.HasValue
                ? c.ClosingTime.Value.ToString(@"hh\:mm") : null,
            About = c.About,
            KnowledgeBase = kb,
            LastUpdatedAt = c.LastUpdatedAt
        };
    }
}
