using FluentValidation;

namespace CashFlow.Transactions.Application.Commands.CreateTransaction;

/// <summary>
/// Validator for CreateTransactionCommand using FluentValidation
/// Implements comprehensive validation following business rules
/// </summary>
public sealed class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
    {
        // Amount validation with business rules
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero")
            .LessThanOrEqualTo(999999999.99m)
            .WithMessage("Amount exceeds maximum allowed value")
            .Must(amount => amount == Math.Round(amount, 2))
            .WithMessage("Amount cannot have more than 2 decimal places");

        // Transaction type validation (case-sensitive)
        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage("Transaction type is required")
            .Must(type => type == "Credit" || type == "Debit")
            .WithMessage("Transaction type must be either 'Credit' or 'Debit' (case-sensitive)");

        // Description validation with enhanced rules
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MinimumLength(3)
            .WithMessage("Description must be at least 3 characters")
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters")
            .Must(desc => !string.IsNullOrWhiteSpace(desc))
            .WithMessage("Description cannot be only whitespace");

        // Transaction date validation
        RuleFor(x => x.TransactionDate)
            .NotEmpty()
            .WithMessage("Transaction date is required")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("Transaction date cannot be in the future")
            .GreaterThanOrEqualTo(DateTime.UtcNow.AddYears(-10))
            .WithMessage("Transaction date cannot be more than 10 years in the past");

        // Idempotency key validation (REQUIRED)
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .WithMessage("Idempotency key is required to prevent duplicate transactions")
            .MinimumLength(16)
            .WithMessage("Idempotency key must be at least 16 characters for security")
            .MaximumLength(100)
            .WithMessage("Idempotency key cannot exceed 100 characters")
            .Matches("^[a-zA-Z0-9-_]+$")
            .WithMessage("Idempotency key can only contain alphanumeric characters, hyphens, and underscores");

        // Reference validation (optional)
        RuleFor(x => x.Reference)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.Reference))
            .WithMessage("Reference cannot exceed 100 characters");
    }
}
