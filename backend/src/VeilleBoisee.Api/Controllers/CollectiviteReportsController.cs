using MediatR;
using Microsoft.AspNetCore.Mvc;
using VeilleBoisee.Api.Auth;
using VeilleBoisee.Application.Reports.Commands;
using VeilleBoisee.Application.Reports.Queries;
using VeilleBoisee.Domain.Enums;

namespace VeilleBoisee.Api.Controllers;

[ApiController]
[Route("api/collectivite/reports")]
public sealed class CollectiviteReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly CollectiviteContext _collectiviteContext;

    public CollectiviteReportsController(IMediator mediator, CollectiviteContext collectiviteContext)
    {
        _mediator = mediator;
        _collectiviteContext = collectiviteContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ReportPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] ReportStatus? status = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetReportsByCollectiviteQuery(
            _collectiviteContext.InseeCodes,
            status,
            from,
            to,
            page,
            pageSize);

        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Error switch
        {
            GetReportsByCollectiviteError.InvalidParameters => BadRequest(new ProblemDetails
            {
                Title = "Invalid pagination parameters",
                Detail = "Page must be ≥ 1 and pageSize must be between 1 and 100.",
                Status = StatusCodes.Status400BadRequest
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReportDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetReportDetailQuery(id), cancellationToken);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Error switch
        {
            GetReportDetailError.NotFound => NotFound(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ReportStatusUpdatedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateReportStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateReportStatusCommand(id, request.NewStatus),
            cancellationToken);

        if (result.IsSuccess)
            return Ok(new ReportStatusUpdatedResponse(result.Value));

        return result.Error switch
        {
            UpdateReportStatusError.ReportNotFound => NotFound(),
            UpdateReportStatusError.InvalidTransition => UnprocessableEntity(new ProblemDetails
            {
                Title = "Invalid status transition",
                Detail = "The requested status transition is not allowed.",
                Status = StatusCodes.Status422UnprocessableEntity
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpGet("export.csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] ReportStatus? status = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new ExportReportsCsvQuery(_collectiviteContext.InseeCodes, status, from, to),
            cancellationToken);

        if (result.IsSuccess)
            return File(result.Value.Content, "text/csv; charset=utf-8", result.Value.FileName);

        return result.Error switch
        {
            ExportReportsCsvError.NoData => NoContent(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}

public sealed record UpdateReportStatusRequest(ReportStatus NewStatus);
public sealed record ReportStatusUpdatedResponse(ReportStatus Status);
