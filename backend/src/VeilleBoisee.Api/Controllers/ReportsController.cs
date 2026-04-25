using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeilleBoisee.Application.Reports.Commands;
using VeilleBoisee.Application.Reports.Queries;
using VeilleBoisee.Domain.Entities;

namespace VeilleBoisee.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ReportsController : ControllerBase
{
    private static readonly HashSet<string> AllowedPhotoMimeTypes =
        ["image/jpeg", "image/png", "image/webp"];
    private const long MaxPhotoSizeBytes = 5 * 1024 * 1024;

    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    [Authorize(Policy = "IsCitizen")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ReportSubmittedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Submit(
        [FromForm] SubmitReportRequest request,
        IFormFile? photo,
        CancellationToken cancellationToken)
    {
        Stream? photoStream = null;
        string? photoMimeType = null;

        if (photo is not null)
        {
            if (photo.Length > MaxPhotoSizeBytes)
                return BadRequest(new ProblemDetails
                {
                    Title = "Photo too large",
                    Detail = "La photo ne peut pas dépasser 5 Mo.",
                    Status = StatusCodes.Status400BadRequest
                });

            if (!AllowedPhotoMimeTypes.Contains(photo.ContentType))
                return BadRequest(new ProblemDetails
                {
                    Title = "Unsupported photo format",
                    Detail = "Formats acceptés : JPEG, PNG, WebP.",
                    Status = StatusCodes.Status400BadRequest
                });

            photoStream = photo.OpenReadStream();
            photoMimeType = photo.ContentType;
        }

        var command = new SubmitReportCommand(
            request.Latitude,
            request.Longitude,
            request.CommuneInsee,
            request.CommuneName,
            request.Description,
            request.ContactEmail,
            User.FindFirst("sub")?.Value,
            photoStream,
            photoMimeType);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
            return StatusCode(StatusCodes.Status201Created, new ReportSubmittedResponse(result.Value));

        return result.Error switch
        {
            SubmitReportError.InvalidCoordinates => BadRequest(new ProblemDetails
            {
                Title = "Invalid coordinates",
                Detail = "Latitude and longitude must fall within France bounds.",
                Status = StatusCodes.Status400BadRequest
            }),
            SubmitReportError.InvalidCommuneInsee => BadRequest(new ProblemDetails
            {
                Title = "Invalid commune INSEE code",
                Status = StatusCodes.Status400BadRequest
            }),
            SubmitReportError.InvalidDescription => UnprocessableEntity(new ProblemDetails
            {
                Title = "Invalid description",
                Detail = $"Description must be between {Report.DescriptionMinLength} and {Report.DescriptionMaxLength} characters.",
                Status = StatusCodes.Status422UnprocessableEntity
            }),
            SubmitReportError.InvalidContactEmail => UnprocessableEntity(new ProblemDetails
            {
                Title = "Invalid contact email",
                Status = StatusCodes.Status422UnprocessableEntity
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpGet("mine")]
    [Authorize(Policy = "IsCitizen")]
    [ProducesResponseType(typeof(IReadOnlyList<MyReportItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst("sub")?.Value!;
        var items = await _mediator.Send(new GetMyReportsQuery(userId), cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}/photo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPhoto(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetReportPhotoQuery(id), cancellationToken);

        if (!result.IsSuccess)
            return NotFound();

        return File(result.Value.Data, result.Value.MimeType);
    }

    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(typeof(ReportStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetReportStatusQuery(id), cancellationToken);

        return result.IsSuccess
            ? Ok(new ReportStatusResponse(result.Value.ToString()))
            : NotFound();
    }
}

public sealed record SubmitReportRequest(
    double Latitude,
    double Longitude,
    string CommuneInsee,
    string CommuneName,
    string Description,
    string ContactEmail);

public sealed record ReportSubmittedResponse(Guid ReportId);
public sealed record ReportStatusResponse(string Status);
