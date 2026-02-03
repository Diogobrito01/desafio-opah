using CashFlow.Transactions.Domain.Entities;
using CashFlow.Transactions.Domain.Enums;
using CashFlow.Transactions.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace CashFlow.Transactions.UnitTests.Domain;

public class TransactionIdempotencyTests
{
    [Fact]
    public void Create_WithValidIdempotencyKey_ShouldCreateTransaction()
    {
        // Arrange
        var idempotencyKey = "valid-key-12345678";
        
        // Act
        var transaction = Transaction.Create(
            100m,
            TransactionType.Credit,
            "Test",
            DateTime.UtcNow,
            idempotencyKey,
            null);
        
        // Assert
        transaction.Should().NotBeNull();
        transaction.IdempotencyKey.Should().Be(idempotencyKey);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyIdempotencyKey_ShouldThrowException(string idempotencyKey)
    {
        // Act
        Action act = () => Transaction.Create(
            100m,
            TransactionType.Credit,
            "Test",
            DateTime.UtcNow,
            idempotencyKey,
            null);
        
        // Assert
        act.Should().Throw<TransactionDomainException>()
            .WithMessage("*Idempotency key*required*");
    }

    [Fact]
    public void Create_WithIdempotencyKeyTooShort_ShouldThrowException()
    {
        // Arrange
        var shortKey = "short123"; // Menos de 16 caracteres
        
        // Act
        Action act = () => Transaction.Create(
            100m,
            TransactionType.Credit,
            "Test",
            DateTime.UtcNow,
            shortKey,
            null);
        
        // Assert
        act.Should().Throw<TransactionDomainException>()
            .WithMessage("*at least 16 characters*");
    }

    [Fact]
    public void Create_WithIdempotencyKeyTooLong_ShouldThrowException()
    {
        // Arrange
        var longKey = new string('a', 101); // Mais de 100 caracteres
        
        // Act
        Action act = () => Transaction.Create(
            100m,
            TransactionType.Credit,
            "Test",
            DateTime.UtcNow,
            longKey,
            null);
        
        // Assert
        act.Should().Throw<TransactionDomainException>()
            .WithMessage("*cannot exceed 100 characters*");
    }

    [Fact]
    public void Create_WithMinimumLengthIdempotencyKey_ShouldSucceed()
    {
        // Arrange
        var key = "1234567890123456"; // Exatamente 16 caracteres
        
        // Act
        var transaction = Transaction.Create(
            100m,
            TransactionType.Credit,
            "Test",
            DateTime.UtcNow,
            key,
            null);
        
        // Assert
        transaction.IdempotencyKey.Should().Be(key);
    }

    [Fact]
    public void Create_WithMaximumLengthIdempotencyKey_ShouldSucceed()
    {
        // Arrange
        var key = new string('a', 100); // Exatamente 100 caracteres
        
        // Act
        var transaction = Transaction.Create(
            100m,
            TransactionType.Credit,
            "Test",
            DateTime.UtcNow,
            key,
            null);
        
        // Assert
        transaction.IdempotencyKey.Should().Be(key);
    }

    [Theory]
    [InlineData("valid-key-with-hyphens-123456")]
    [InlineData("valid_key_with_underscores_123456")]
    [InlineData("validKeyWithMixedCase123456")]
    [InlineData("1234567890123456")]
    public void Create_WithValidIdempotencyKeyFormats_ShouldSucceed(string idempotencyKey)
    {
        // Act
        var transaction = Transaction.Create(
            100m,
            TransactionType.Credit,
            "Test",
            DateTime.UtcNow,
            idempotencyKey,
            null);
        
        // Assert
        transaction.IdempotencyKey.Should().Be(idempotencyKey);
    }
}
