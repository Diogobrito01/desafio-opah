using CashFlow.Consolidation.Application.Commands.ProcessTransaction;
using CashFlow.Consolidation.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace CashFlow.Consolidation.API.Controllers;

/// <summary>
/// Administrative endpoints for system maintenance
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    private readonly IDailyConsolidationRepository _consolidationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IMediator mediator,
        IConfiguration configuration,
        IDailyConsolidationRepository consolidationRepository,
        IUnitOfWork unitOfWork,
        ILogger<AdminController> logger)
    {
        _mediator = mediator;
        _configuration = configuration;
        _consolidationRepository = consolidationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Recalculates consolidation for a specific date by fetching all transactions from the Transactions database
    /// </summary>
    /// <param name="date">Date to recalculate (format: YYYY-MM-DD or DD-MM-YYYY)</param>
    /// <returns>Result of recalculation</returns>
    [HttpPost("recalculate/{date}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecalculateConsolidation(DateTime date)
    {
        try
        {
            _logger.LogInformation("Starting recalculation for date {Date}", date.Date);

            var transactionsConnectionString = _configuration.GetConnectionString("TransactionsConnection");
            
            if (string.IsNullOrEmpty(transactionsConnectionString))
            {
                return BadRequest(new { error = "TransactionsConnection not configured" });
            }

            var transactions = new List<(Guid Id, decimal Amount, string Type, DateTime TransactionDate)>();

            // Fetch all transactions for the date from Transactions database
            await using (var conn = new NpgsqlConnection(transactionsConnectionString))
            {
                await conn.OpenAsync();

                var startOfDay = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
                var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

                var sql = @"
                    SELECT id, amount, type, transaction_date 
                    FROM transactions 
                    WHERE transaction_date >= @StartOfDay AND transaction_date <= @EndOfDay
                    ORDER BY created_at";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("StartOfDay", startOfDay);
                cmd.Parameters.AddWithValue("EndOfDay", endOfDay);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    transactions.Add((
                        reader.GetGuid(0),
                        reader.GetDecimal(1),
                        reader.GetInt32(2) == 1 ? "Credit" : "Debit",
                        reader.GetDateTime(3)
                    ));
                }
            }

            _logger.LogInformation("Found {Count} transactions for date {Date}", transactions.Count, date.Date);

            // Delete existing consolidation for this date to start fresh
            var existingConsolidation = await _consolidationRepository.GetByDateAsync(date.Date);
            if (existingConsolidation != null)
            {
                _logger.LogInformation("Deleting existing consolidation for date {Date}", date.Date);
                
                // Delete via SQL to ensure it's removed
                var consolidationConnectionString = _configuration.GetConnectionString("DefaultConnection");
                await using (var conn = new NpgsqlConnection(consolidationConnectionString))
                {
                    await conn.OpenAsync();
                    var deleteSql = "DELETE FROM daily_consolidations WHERE date = @Date";
                    await using var deleteCmd = new NpgsqlCommand(deleteSql, conn);
                    deleteCmd.Parameters.AddWithValue("Date", DateTime.SpecifyKind(date.Date, DateTimeKind.Utc));
                    await deleteCmd.ExecuteNonQueryAsync();
                }
                
                _logger.LogInformation("Existing consolidation deleted");
            }

            // Process each transaction through the consolidation handler
            foreach (var (id, amount, type, transactionDate) in transactions)
            {
                var command = new ProcessTransactionCommand
                {
                    TransactionId = id,
                    Amount = amount,
                    Type = type,
                    TransactionDate = transactionDate
                };

                var result = await _mediator.Send(command);
                
                if (result.IsFailure)
                {
                    _logger.LogWarning("Failed to process transaction {TransactionId}: {Error}", 
                        id, result.Error.Message);
                }
            }

            return Ok(new
            {
                message = "Recalculation completed successfully",
                date = date.Date,
                transactionsProcessed = transactions.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating consolidation for date {Date}", date);
            return BadRequest(new
            {
                error = "Recalculation failed",
                message = ex.Message
            });
        }
    }
}
