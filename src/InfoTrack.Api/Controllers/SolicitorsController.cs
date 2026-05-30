using InfoTrack.Core.Interfaces;
using InfoTrack.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace InfoTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class SolicitorsController : Controller
{
    private static readonly string[] DefaultLocations =
    [
        "London",
        "Birmingham",
        "Leeds",
        "Manchester",
        "Sheffield",
        "Bradford",
        "Liverpool",
        "Bristol"
    ];

    private readonly ISolicitorRepository _repository;
    private readonly ILogger<SolicitorsController> _logger;

    public SolicitorsController(ISolicitorRepository repository, ILogger<SolicitorsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet("locations")]
    public IActionResult GetLocations() => Ok(DefaultLocations);

    // refresh=true bypasses the 24h cache and re-scrapes
    [HttpGet]
    [ProducesResponseType<IEnumerable<LocationResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSolicitors([FromQuery] string[] locations, [FromQuery] bool refresh = false, CancellationToken cancellationToken = default)
    {
        if (locations.Length == 0)
            return BadRequest(new { error = "At least one location is required." });

        _logger.LogInformation(
            "Request for {Count} location(s): {Locations} (refresh={Refresh})",
            locations.Length, string.Join(", ", locations), refresh);

        var request = new ScrapeRequest { Locations = locations, ForceRefresh = refresh };
        var results = await _repository.GetByLocationsAsync(request, cancellationToken);

        return Ok(results);
    }

    [HttpDelete("cache/{location}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult InvalidateCache(string location)
    {
        _repository.InvalidateCache(location);
        return NoContent();
    }

    [HttpDelete("cache")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult InvalidateAllCaches()
    {
        _repository.InvalidateAllCaches();
        return NoContent();
    }
}
