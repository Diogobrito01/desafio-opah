using CashFlow.Transactions.Application.Commands.CreateTransaction;
using CashFlow.Transactions.Application.Queries.GetTransactionById;
using CashFlow.Transactions.Application.Queries.GetTransactionsByDate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Transactions.API.Controllers;

/// <summary>
/// Controller for managing financial transactions
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TransactionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(IMediator mediator, ILogger<TransactionsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new transaction with idempotency support
    /// </summary>
    /// <remarks>
    /// **Idempotency**: This endpoint requires an `idempotencyKey` to prevent duplicate transactions.
    /// If a transaction with the same idempotency key already exists, the existing transaction is returned.
    /// 
    /// **Idempotency Key Requirements**:
    /// - Must be unique per transaction
    /// - Minimum 16 characters
    /// - Can contain alphanumeric characters, hyphens, and underscores
    /// - Recommended format: `{client-id}-{timestamp}-{nonce}` or a UUID
    /// 
    /// **Duplicate Detection**: The system automatically detects potential duplicates based on:
    /// - Same amount and type within 5 minutes (high risk)
    /// - Same amount, type, and date (medium risk)
    /// - Similar description using Levenshtein distance (variable risk)
    /// - Same reference number (high risk)
    /// 
    /// If potential duplicates are found (similarity score ≥ 70%), they will be included in the response
    /// as warnings in the `potentialDuplicates` field. The transaction is still created, but you should
    /// review these warnings.
    /// 
    /// **Sample Request**:
    /// ```json
    /// {
    ///   "amount": 100.50,
    ///   "type": "Credit",
    ///   "description": "Payment received",
    ///   "transactionDate": "2026-02-03",
    ///   "idempotencyKey": "client-123-20260203120000-abc123",
    ///   "reference": "INV-001"
    /// }
    /// ```
    /// </remarks>
    /// <param name="command">Transaction details including required idempotency key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created transaction, with potential duplicate warnings if detected</returns>
    /// <response code="201">Transaction created successfully. Check `potentialDuplicates` field for warnings.</response>
    /// <response code="400">Invalid request data (missing idempotency key, invalid type, etc.)</response>
    /// <response code="429">Too many requests - rate limit exceeded</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CreateTransaction(
        [FromBody] CreateTransactionCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new transaction");

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new
            {
                error = result.Error.Code,
                message = result.Error.Message
            });
        }

        var response = result.Value;

        // If transaction already existed (idempotency), return 200 OK instead of 201 Created
        if (!response.IsNewTransaction)
        {
            return Ok(response);
        }

        // New transaction created, return 201 Created
        return CreatedAtAction(
            nameof(GetTransactionById),
            new { id = response.Transaction.Id },
            response);
    }

    /// <summary>
    /// Gets a transaction by ID
    /// </summary>
    /// <param name="id">Transaction ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transaction if found</returns>
    /// <response code="200">Transaction found</response>
    /// <response code="404">Transaction not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactionById(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving transaction {TransactionId}", id);

        var result = await _mediator.Send(new GetTransactionByIdQuery(id), cancellationToken);

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
    /// Gets all transactions for a specific date
    /// </summary>
    /// <param name="date">Date to filter transactions (format: yyyy-MM-dd)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of transactions for the specified date</returns>
    /// <response code="200">Transactions retrieved successfully</response>
    /// <response code="400">Invalid date format</response>
    [HttpGet("by-date")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTransactionsByDate(
        [FromQuery] DateTime date,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving transactions for date {Date}", date);

        var result = await _mediator.Send(new GetTransactionsByDateQuery(date), cancellationToken);

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
            date = date.Date,
            count = result.Value.Count,
            transactions = result.Value
        });
    }
}
