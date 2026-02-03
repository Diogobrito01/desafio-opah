using CashFlow.BuildingBlocks.Core.Results;
using CashFlow.Consolidation.Domain.Entities;
using CashFlow.Consolidation.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CashFlow.Consolidation.Application.Commands.ProcessTransaction;

/// <summary>
/// Handler for ProcessTransactionCommand
/// Processes transaction events and updates daily consolidation with cache invalidation
/// </summary>
public sealed class ProcessTransactionCommandHandler : IRequestHandler<ProcessTransactionCommand, Result>
{
    private readonly IDailyConsolidationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ProcessTransactionCommandHandler> _logger;

    public ProcessTransactionCommandHandler(
        IDailyConsolidationRepository repository,
        IUnitOfWork unitOfWork,
        IDistributedCache cache,
        ILogger<ProcessTransactionCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result> Handle(ProcessTransactionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Processing transaction {TransactionId} for date {Date}",
                request.TransactionId,
                request.TransactionDate.Date);

            var date = request.TransactionDate.Date;

            // Get or create consolidation for the date
            var consolidation = await _repository.GetByDateAsync(date, cancellationToken);

            bool isNew = false;
            if (consolidation is null)
            {
                consolidation = DailyConsolidation.Create(date);
                await _repository.AddAsync(consolidation, cancellationToken);
                isNew = true;
            }

            // Update consolidation based on transaction type
            if (request.Type.Equals("Credit", StringComparison.OrdinalIgnoreCase))
            {
                consolidation.AddCredit(request.Amount);
            }
            else if (request.Type.Equals("Debit", StringComparison.OrdinalIgnoreCase))
            {
                consolidation.AddDebit(request.Amount);
            }
            else
            {
                return Result.Failure(
                    Error.Validation("Transaction.InvalidType", $"Invalid transaction type: {request.Type}"));
            }

            // Only call Update if it's an existing entity (EF Core will track changes automatically)
            if (!isNew)
            {
                _repository.Update(consolidation);
            }
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Invalidate cache for this date
            await _cache.RemoveAsync($"consolidation:{date:yyyy-MM-dd}", cancellationToken);

            _logger.LogInformation(
                "Transaction {TransactionId} processed successfully. New balance: {Balance}",
                request.TransactionId,
                consolidation.Balance);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transaction {TransactionId}", request.TransactionId);
            return Result.Failure(
                Error.Failure("Consolidation.ProcessFailed", "An error occurred while processing the transaction"));
        }
    }
}
