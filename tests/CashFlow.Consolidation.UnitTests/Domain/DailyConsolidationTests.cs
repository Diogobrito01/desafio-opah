using CashFlow.Consolidation.Domain.Entities;
using CashFlow.Consolidation.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace CashFlow.Consolidation.UnitTests.Domain;

/// <summary>
/// Unit tests for DailyConsolidation entity
/// Tests domain logic and business rules
/// </summary>
public class DailyConsolidationTests
{
    [Fact]
    public void Create_ShouldInitializeWithZeroValues()
    {
        // Arrange
        var date = new DateTime(2024, 1, 15);

        // Act
        var consolidation = DailyConsolidation.Create(date);

        // Assert
        consolidation.Should().NotBeNull();
        consolidation.Id.Should().NotBeEmpty();
        consolidation.Date.Should().Be(date.Date);
        consolidation.TotalCredits.Should().Be(0);
        consolidation.TotalDebits.Should().Be(0);
        consolidation.Balance.Should().Be(0);
        consolidation.TransactionCount.Should().Be(0);
        consolidation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AddCredit_ShouldIncreaseCreditsAndBalance()
    {
        // Arrange
        var consolidation = DailyConsolidation.Create(DateTime.UtcNow);
        var creditAmount = 100.50m;

        // Act
        consolidation.AddCredit(creditAmount);

        // Assert
        consolidation.TotalCredits.Should().Be(creditAmount);
        consolidation.Balance.Should().Be(creditAmount);
        consolidation.TransactionCount.Should().Be(1);
    }

    [Fact]
    public void AddDebit_ShouldIncreaseDebitsAndDecreaseBalance()
    {
        // Arrange
        var consolidation = DailyConsolidation.Create(DateTime.UtcNow);
        var debitAmount = 50.25m;

        // Act
        consolidation.AddDebit(debitAmount);

        // Assert
        consolidation.TotalDebits.Should().Be(debitAmount);
        consolidation.Balance.Should().Be(-debitAmount);
        consolidation.TransactionCount.Should().Be(1);
    }

    [Fact]
    public void AddMultipleTransactions_ShouldCalculateCorrectBalance()
    {
        // Arrange
        var consolidation = DailyConsolidation.Create(DateTime.UtcNow);

        // Act
        consolidation.AddCredit(100m);
        consolidation.AddCredit(50m);
        consolidation.AddDebit(30m);
        consolidation.AddDebit(20m);

        // Assert
        consolidation.TotalCredits.Should().Be(150m);
        consolidation.TotalDebits.Should().Be(50m);
        consolidation.Balance.Should().Be(100m);
        consolidation.TransactionCount.Should().Be(4);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void AddCredit_WithNegativeAmount_ShouldThrowException(decimal amount)
    {
        // Arrange
        var consolidation = DailyConsolidation.Create(DateTime.UtcNow);

        // Act
        Action act = () => consolidation.AddCredit(amount);

        // Assert
        act.Should().Throw<ConsolidationDomainException>()
            .WithMessage("Amount cannot be negative");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void AddDebit_WithNegativeAmount_ShouldThrowException(decimal amount)
    {
        // Arrange
        var consolidation = DailyConsolidation.Create(DateTime.UtcNow);

        // Act
        Action act = () => consolidation.AddDebit(amount);

        // Assert
        act.Should().Throw<ConsolidationDomainException>()
            .WithMessage("Amount cannot be negative");
    }

    [Fact]
    public void Recalculate_ShouldUpdateAllValues()
    {
        // Arrange
        var consolidation = DailyConsolidation.Create(DateTime.UtcNow);
        consolidation.AddCredit(100m);

        // Act
        consolidation.Recalculate(totalCredits: 200m, totalDebits: 50m, transactionCount: 5);

        // Assert
        consolidation.TotalCredits.Should().Be(200m);
        consolidation.TotalDebits.Should().Be(50m);
        consolidation.Balance.Should().Be(150m);
        consolidation.TransactionCount.Should().Be(5);
    }

    [Fact]
    public void Recalculate_WithNegativeTransactionCount_ShouldThrowException()
    {
        // Arrange
        var consolidation = DailyConsolidation.Create(DateTime.UtcNow);

        // Act
        Action act = () => consolidation.Recalculate(100m, 50m, -1);

        // Assert
        act.Should().Throw<ConsolidationDomainException>()
            .WithMessage("Transaction count cannot be negative");
    }
}
