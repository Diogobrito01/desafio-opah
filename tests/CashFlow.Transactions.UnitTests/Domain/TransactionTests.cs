using CashFlow.Transactions.Domain.Entities;
using CashFlow.Transactions.Domain.Enums;
using CashFlow.Transactions.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace CashFlow.Transactions.UnitTests.Domain;

/// <summary>
/// Unit tests for Transaction entity
/// Tests domain logic and business rules
/// </summary>
public class TransactionTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateTransaction()
    {
        // Arrange
        var amount = 100.50m;
        var type = TransactionType.Credit;
        var description = "Test transaction";
        var transactionDate = DateTime.UtcNow;
        var idempotencyKey = "test-key-1234567890";

        // Act
        var transaction = Transaction.Create(amount, type, description, transactionDate, idempotencyKey, null);

        // Assert
        transaction.Should().NotBeNull();
        transaction.Id.Should().NotBeEmpty();
        transaction.Amount.Should().Be(amount);
        transaction.Type.Should().Be(type);
        transaction.Description.Should().Be(description);
        transaction.TransactionDate.Should().Be(transactionDate);
        transaction.IdempotencyKey.Should().Be(idempotencyKey);
        transaction.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(-100.50)]
    public void Create_WithInvalidAmount_ShouldThrowException(decimal amount)
    {
        // Arrange
        var type = TransactionType.Credit;
        var description = "Test transaction";
        var transactionDate = DateTime.UtcNow;
        var idempotencyKey = "test-key-1234567890";

        // Act
        Action act = () => Transaction.Create(amount, type, description, transactionDate, idempotencyKey, null);

        // Assert
        act.Should().Throw<TransactionDomainException>()
            .WithMessage("Transaction amount must be greater than zero");
    }

    [Fact]
    public void Create_WithAmountExceedingMaxValue_ShouldThrowException()
    {
        // Arrange
        var amount = 1000000000m;
        var type = TransactionType.Credit;
        var description = "Test transaction";
        var transactionDate = DateTime.UtcNow;
        var idempotencyKey = "test-key-1234567890";

        // Act
        Action act = () => Transaction.Create(amount, type, description, transactionDate, idempotencyKey, null);

        // Assert
        act.Should().Throw<TransactionDomainException>()
            .WithMessage("Transaction amount exceeds maximum allowed value");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null!)]
    public void Create_WithInvalidDescription_ShouldThrowException(string description)
    {
        // Arrange
        var amount = 100m;
        var type = TransactionType.Credit;
        var transactionDate = DateTime.UtcNow;
        var idempotencyKey = "test-key-1234567890";

        // Act
        Action act = () => Transaction.Create(amount, type, description, transactionDate, idempotencyKey, null);

        // Assert
        act.Should().Throw<TransactionDomainException>()
            .WithMessage("Transaction description is required");
    }

    [Fact]
    public void GetSignedAmount_ForCredit_ShouldReturnPositiveAmount()
    {
        // Arrange
        var amount = 100m;
        var transaction = Transaction.Create(
            amount,
            TransactionType.Credit,
            "Test credit",
            DateTime.UtcNow,
            "test-key-1234567890",
            null);

        // Act
        var signedAmount = transaction.GetSignedAmount();

        // Assert
        signedAmount.Should().Be(amount);
    }

    [Fact]
    public void GetSignedAmount_ForDebit_ShouldReturnNegativeAmount()
    {
        // Arrange
        var amount = 100m;
        var transaction = Transaction.Create(
            amount,
            TransactionType.Debit,
            "Test debit",
            DateTime.UtcNow,
            "test-key-1234567890",
            null);

        // Act
        var signedAmount = transaction.GetSignedAmount();

        // Assert
        signedAmount.Should().Be(-amount);
    }
}
