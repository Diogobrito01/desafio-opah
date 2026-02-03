using CashFlow.Transactions.Application.Commands.CreateTransaction;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace CashFlow.Transactions.UnitTests.Application;

public class CreateTransactionCommandValidatorTests
{
    private readonly CreateTransactionCommandValidator _validator;

    public CreateTransactionCommandValidatorTests()
    {
        _validator = new CreateTransactionCommandValidator();
    }

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var command = new CreateTransactionCommand
        {
            Amount = 100.00m,
            Type = "Credit",
            Description = "Venda de produto",
            TransactionDate = DateTime.UtcNow,
            IdempotencyKey = "valid-key-12345678",
            Reference = "REF-001"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(-100.50)]
    public void Validate_WithInvalidAmount_ShouldHaveValidationError(decimal amount)
    {
        // Arrange
        var command = new CreateTransactionCommand
        {
            Amount = amount,
            Type = "Credit",
            Description = "Test",
            TransactionDate = DateTime.UtcNow,
            IdempotencyKey = "valid-key-12345678"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyDescription_ShouldHaveValidationError(string description)
    {
        // Arrange
        var command = new CreateTransactionCommand
        {
            Amount = 100m,
            Type = "Credit",
            Description = description,
            TransactionDate = DateTime.UtcNow,
            IdempotencyKey = "valid-key-12345678"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WithDescriptionTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateTransactionCommand
        {
            Amount = 100m,
            Type = "Credit",
            Description = new string('a', 501), // Mais de 500 caracteres
            TransactionDate = DateTime.UtcNow,
            IdempotencyKey = "valid-key-12345678"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyType_ShouldHaveValidationError(string type)
    {
        // Arrange
        var command = new CreateTransactionCommand
        {
            Amount = 100m,
            Type = type,
            Description = "Test",
            TransactionDate = DateTime.UtcNow,
            IdempotencyKey = "valid-key-12345678"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Theory]
    [InlineData("credit")]
    [InlineData("CREDIT")]
    [InlineData("Credito")]
    [InlineData("Invalid")]
    public void Validate_WithInvalidType_ShouldHaveValidationError(string type)
    {
        // Arrange
        var command = new CreateTransactionCommand
        {
            Amount = 100m,
            Type = type,
            Description = "Test",
            TransactionDate = DateTime.UtcNow,
            IdempotencyKey = "valid-key-12345678"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Theory]
    [InlineData("Credit")]
    [InlineData("Debit")]
    public void Validate_WithValidType_ShouldNotHaveValidationError(string type)
    {
        // Arrange
        var command = new CreateTransactionCommand
        {
            Amount = 100m,
            Type = type,
            Description = "Test",
            TransactionDate = DateTime.UtcNow,
            IdempotencyKey = "valid-key-12345678"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Type);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyIdempotencyKey_ShouldHaveValidationError(string idempotencyKey)
    {
        // Arrange
        var command = new CreateTransactionCommand
        {
            Amount = 100m,
            Type = "Credit",
            Description = "Test",
            TransactionDate = DateTime.UtcNow,
            IdempotencyKey = idempotencyKey
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey);
    }

    [Fact]
    public void Validate_WithIdempotencyKeyTooShort_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateTransactionCommand
        {
            Amount = 100m,
            Type = "Credit",
            Description = "Test",
            TransactionDate = DateTime.UtcNow,
            IdempotencyKey = "short123" // Menos de 16 caracteres
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey);
    }

    [Fact]
    public void Validate_WithIdempotencyKeyTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateTransactionCommand
        {
            Amount = 100m,
            Type = "Credit",
            Description = "Test",
            TransactionDate = DateTime.UtcNow,
            IdempotencyKey = new string('a', 101) // Mais de 100 caracteres
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey);
    }

    [Fact]
    public void Validate_WithReferenceTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateTransactionCommand
        {
            Amount = 100m,
            Type = "Credit",
            Description = "Test",
            TransactionDate = DateTime.UtcNow,
            IdempotencyKey = "valid-key-12345678",
            Reference = new string('a', 101) // Mais de 100 caracteres
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Reference);
    }

    [Fact]
    public void Validate_WithNullReference_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = new CreateTransactionCommand
        {
            Amount = 100m,
            Type = "Credit",
            Description = "Test",
            TransactionDate = DateTime.UtcNow,
            IdempotencyKey = "valid-key-12345678",
            Reference = null
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Reference);
    }

    [Fact]
    public void Validate_WithMinimumLengthIdempotencyKey_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = new CreateTransactionCommand
        {
            Amount = 100m,
            Type = "Credit",
            Description = "Test",
            TransactionDate = DateTime.UtcNow,
            IdempotencyKey = "1234567890123456" // Exatamente 16 caracteres
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.IdempotencyKey);
    }

    [Fact]
    public void Validate_WithMaximumLengthIdempotencyKey_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = new CreateTransactionCommand
        {
            Amount = 100m,
            Type = "Credit",
            Description = "Test",
            TransactionDate = DateTime.UtcNow,
            IdempotencyKey = new string('a', 100) // Exatamente 100 caracteres
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.IdempotencyKey);
    }
}
