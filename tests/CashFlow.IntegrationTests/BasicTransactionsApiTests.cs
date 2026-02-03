using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace CashFlow.IntegrationTests;

/// <summary>
/// Basic integration tests for Transactions API
/// Tests HTTP layer without external dependencies (InMemory DB)
/// </summary>
public class BasicTransactionsApiTests : IClassFixture<TransactionsApiFactory>
{
    private readonly HttpClient _client;

    public BasicTransactionsApiTests(TransactionsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateTransaction_WithValidData_ShouldReturn201Created()
    {
        // Arrange
        var request = new
        {
            amount = 100.50m,
            type = "Credit",
            description = "Integration test transaction",
            transactionDate = DateTime.UtcNow,
            idempotencyKey = $"test-key-{Guid.NewGuid()}",
            reference = "TEST-001"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var result = await response.Content.ReadFromJsonAsync<TransactionResponseDto>();
        result.Should().NotBeNull();
        result!.IsNewTransaction.Should().BeTrue();
        result.Transaction.Amount.Should().Be(request.amount);
        result.Transaction.Type.Should().Be(request.type);
    }

    [Fact]
    public async Task CreateTransaction_WithInvalidAmount_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new
        {
            amount = -10m,
            type = "Credit",
            description = "Invalid transaction",
            transactionDate = DateTime.UtcNow,
            idempotencyKey = $"test-key-{Guid.NewGuid()}"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTransaction_WithoutIdempotencyKey_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new
        {
            amount = 100m,
            type = "Credit",
            description = "Test without idempotency key",
            transactionDate = DateTime.UtcNow
            // idempotencyKey ausente
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTransaction_WithShortIdempotencyKey_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new
        {
            amount = 100m,
            type = "Credit",
            description = "Test with short key",
            transactionDate = DateTime.UtcNow,
            idempotencyKey = "short" // Menos de 16 caracteres
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTransaction_WithInvalidType_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new
        {
            amount = 100m,
            type = "InvalidType",
            description = "Test with invalid type",
            transactionDate = DateTime.UtcNow,
            idempotencyKey = $"test-key-{Guid.NewGuid()}"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private class TransactionResponseDto
    {
        public TransactionDto Transaction { get; set; } = default!;
        public bool IsNewTransaction { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    private class TransactionDto
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
