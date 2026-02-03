using CashFlow.Consolidation.Application.Queries.GetConsolidationReport;
using CashFlow.Consolidation.Application.Queries.GetDailyConsolidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Consolidation.API.Controllers;

/// <summary>
/// Controller for retrieving consolidated balance information
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ConsolidationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ConsolidationController> _logger;

    public ConsolidationController(IMediator mediator, ILogger<ConsolidationController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets the consolidated balance for a specific date
    /// </summary>
    /// <param name="date">Date to retrieve consolidation (format: yyyy-MM-dd)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The daily consolidation if found</returns>
    /// <response code="200">Consolidation found</response>
    /// <response code="404">Consolidation not found</response>
    /// <response code="429">Too many requests</response>
    [HttpGet("daily")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetDailyConsolidation(
        [FromQuery] DateTime date,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving daily consolidation for {Date}", date);

        var result = await _mediator.Send(new GetDailyConsolidationQuery(date), cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new
            {
                error = result.Error.Code,
                message = result.Error.Message
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets a consolidation report for a date range
    /// </summary>
    /// <param name="startDate">Start date (format: yyyy-MM-dd)</param>
    /// <param name="endDate">End date (format: yyyy-MM-dd)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of daily consolidations for the specified date range</returns>
    /// <response code="200">Report retrieved successfully</response>
    /// <response code="400">Invalid date range</response>
    /// <response code="429">Too many requests</response>
    [HttpGet("report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetConsolidationReport(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving consolidation report from {StartDate} to {EndDate}", startDate, endDate);

        var result = await _mediator.Send(
            new GetConsolidationReportQuery(startDate, endDate),
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new
            {
                error = result.Error.Code,
                message = result.Error.Message
            });
        }

        return Ok(new
        {
            startDate = startDate.Date,
            endDate = endDate.Date,
            count = result.Value.Count,
            consolidations = result.Value
        });
    }
}
