using AutoMapper;
using CashFlow.BuildingBlocks.Core.Results;
using CashFlow.Transactions.Application.DTOs;
using CashFlow.Transactions.Application.IntegrationEvents;
using CashFlow.Transactions.Domain.Entities;
using CashFlow.Transactions.Domain.Enums;
using CashFlow.Transactions.Domain.Repositories;
using CashFlow.Transactions.Domain.Services;
using CashFlow.BuildingBlocks.EventBus;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CashFlow.Transactions.Application.Commands.CreateTransaction;

/// <summary>
/// Handler for CreateTransactionCommand
/// Implements CQRS pattern with MediatR
/// Includes idempotency and duplicate detection
/// </summary>
public sealed class CreateTransactionCommandHandler
    : IRequestHandler<CreateTransactionCommand, Result<TransactionResponseDto>>
{
    private readonly ITransactionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;
    private readonly ITransactionDeduplicationService _deduplicationService;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateTransactionCommandHandler> _logger;

    public CreateTransactionCommandHandler(
        ITransactionRepository repository,
        IUnitOfWork unitOfWork,
        IEventBus eventBus,
        ITransactionDeduplicationService deduplicationService,
        IMapper mapper,
        ILogger<CreateTransactionCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
        _deduplicationService = deduplicationService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<TransactionResponseDto>> Handle(
        CreateTransactionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Creating transaction: Amount={Amount}, Type={Type}, Date={Date}, IdempotencyKey={IdempotencyKey}",
                request.Amount,
                request.Type,
                request.TransactionDate,
                request.IdempotencyKey);

            // Step 1: Check idempotency - if transaction with this key already exists, return it
            var existingTransaction = await _repository.GetByIdempotencyKeyAsync(
                request.IdempotencyKey,
                cancellationToken);

            if (existingTransaction is not null)
            {
                _logger.LogWarning(
                    "Transaction with IdempotencyKey={IdempotencyKey} already exists. Returning existing transaction {TransactionId}",
                    request.IdempotencyKey,
                    existingTransaction.Id);

                var existingDto = _mapper.Map<TransactionDto>(existingTransaction);
                
                // Return existing transaction with metadata indicating it's not new
                var existingResponse = new TransactionResponseDto
                {
                    Transaction = existingDto,
                    IsNewTransaction = false,
                    Message = $"Transaction already exists with this idempotency key. Returning existing transaction (ID: {existingTransaction.Id})."
                };
                
                return Result.Success(existingResponse);
            }

            // Step 2: Parse and validate transaction type
            if (!Enum.TryParse<TransactionType>(request.Type, true, out var transactionType))
            {
                return Result.Failure<TransactionResponseDto>(
                    Error.Validation("Transaction.InvalidType", "Invalid transaction type. Must be 'Credit' or 'Debit'"));
            }

            // Step 3: Check for potential duplicates (warning only, doesn't block creation)
            var potentialDuplicates = await _deduplicationService.FindPotentialDuplicatesAsync(
                request.Amount,
                request.Type,
                request.Description,
                request.TransactionDate,
                request.Reference,
                cancellationToken);

            if (potentialDuplicates.Any())
            {
                _logger.LogWarning(
                    "Found {Count} potential duplicate(s) for transaction with IdempotencyKey={IdempotencyKey}",
                    potentialDuplicates.Count,
                    request.IdempotencyKey);
            }

            // Step 4: Create domain entity using factory method
            var transaction = Transaction.Create(
                request.Amount,
                transactionType,
                request.Description,
                request.TransactionDate,
                request.IdempotencyKey,
                request.Reference);

            // Step 5: Persist transaction
            await _repository.AddAsync(transaction, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Transaction created successfully with Id: {TransactionId}, IdempotencyKey: {IdempotencyKey}",
                transaction.Id,
                transaction.IdempotencyKey);

            // Step 6: Map to DTO and include duplicate warnings
            var dto = _mapper.Map<TransactionDto>(transaction);
            
            if (potentialDuplicates.Any())
            {
                var duplicateWarnings = potentialDuplicates
                    .Select(d => new DuplicateWarningDto
                    {
                        TransactionId = d.TransactionId,
                        Amount = d.Amount,
                        Type = d.Type,
                        Description = d.Description,
                        TransactionDate = d.TransactionDate,
                        CreatedAt = d.CreatedAt,
                        SimilarityScore = d.SimilarityScore,
                        Reason = d.Reason
                    })
                    .ToList();

                dto = dto with { PotentialDuplicates = duplicateWarnings };
            }

            // Step 7: Publish integration event (don't fail if event publishing fails)
            await PublishTransactionCreatedEventAsync(transaction, cancellationToken);

            // Return new transaction with metadata indicating it's newly created
            var response = new TransactionResponseDto
            {
                Transaction = dto,
                IsNewTransaction = true,
                Message = potentialDuplicates.Any() 
                    ? $"Transaction created successfully. Warning: {potentialDuplicates.Count} potential duplicate(s) detected."
                    : "Transaction created successfully."
            };

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transaction with IdempotencyKey={IdempotencyKey}", request.IdempotencyKey);
            return Result.Failure<TransactionResponseDto>(
                Error.Failure("Transaction.CreateFailed", "An error occurred while creating the transaction"));
        }
    }

    private async Task PublishTransactionCreatedEventAsync(
        Transaction transaction,
        CancellationToken cancellationToken)
    {
        try
        {
            var integrationEvent = new TransactionCreatedIntegrationEvent
            {
                EventId = Guid.NewGuid(),
                OccurredOn = DateTime.UtcNow,
                TransactionId = transaction.Id,
                Amount = transaction.Amount,
                Type = transaction.Type.ToString(),
                TransactionDate = transaction.TransactionDate
            };

            await _eventBus.PublishAsync(integrationEvent, cancellationToken);
            _logger.LogInformation("Integration event published for transaction {TransactionId}", transaction.Id);
        }
        catch (Exception ex)
        {
            // Log but don't fail the transaction creation
            _logger.LogError(ex, "Failed to publish integration event for transaction {TransactionId}", transaction.Id);
        }
    }
}
