using MediatR;
using Microsoft.AspNetCore.Mvc;
using VeilleBoisee.Application.Reports.Commands;
using VeilleBoisee.Application.Reports.Queries;
using VeilleBoisee.Domain.Entities;

namespace VeilleBoisee.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    [ProducesResponseType(typeof(ReportSubmittedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Submit(
        [FromBody] SubmitReportRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SubmitReportCommand(
            request.Latitude,
            request.Longitude,
            request.CommuneInsee,
            request.CommuneName,
            request.Description,
            request.ContactEmail);

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
