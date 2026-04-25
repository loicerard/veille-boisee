using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VeilleBoisee.Api.RateLimiting;
using VeilleBoisee.Application.Communes.Queries;

namespace VeilleBoisee.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CommunesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommunesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [EnableRateLimiting(RateLimitingPolicies.Public)]
    [ProducesResponseType(typeof(CommuneResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetByCoordinates(
        [FromQuery] double lat,
        [FromQuery] double lon,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCommuneByCoordinatesQuery(lat, lon), cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(new CommuneResponse(result.Value.CodeInsee.Value, result.Value.Name));
        }

        return result.Error switch
        {
            GetCommuneByCoordinatesError.InvalidCoordinates => BadRequest(new ProblemDetails
            {
                Title = "Invalid coordinates",
                Detail = "Latitude and longitude must fall within France bounds.",
                Status = StatusCodes.Status400BadRequest
            }),
            GetCommuneByCoordinatesError.CommuneNotFound => NotFound(),
            GetCommuneByCoordinatesError.UpstreamUnavailable => StatusCode(
                StatusCodes.Status502BadGateway,
                new ProblemDetails
                {
                    Title = "Upstream geocoding service unavailable",
                    Status = StatusCodes.Status502BadGateway
                }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}

public sealed record CommuneResponse(string CodeInsee, string Name);
