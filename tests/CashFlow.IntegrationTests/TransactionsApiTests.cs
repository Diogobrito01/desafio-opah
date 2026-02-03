using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace CashFlow.IntegrationTests;

/// <summary>
/// Integration tests for Transactions API
/// Tests the complete flow from HTTP request to database
/// </summary>
public class TransactionsApiTests : IClassFixture<TransactionsApiFactory>
{
    private readonly HttpClient _client;

    public TransactionsApiTests(TransactionsApiFactory factory)
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
        result.Transaction.Description.Should().Be(request.description);
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
    public async Task GetTransaction_WhenExists_ShouldReturn200OK()
    {
        // Arrange - Create a transaction first
        var createRequest = new
        {
            amount = 50.00m,
            type = "Debit",
            description = "Test transaction for retrieval",
            transactionDate = DateTime.UtcNow,
            idempotencyKey = $"test-key-{Guid.NewGuid()}"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/transactions", createRequest);
        var createdResult = await createResponse.Content.ReadFromJsonAsync<TransactionResponseDto>();

        // Act
        var response = await _client.GetAsync($"/api/transactions/{createdResult!.Transaction.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var transaction = await response.Content.ReadFromJsonAsync<TransactionDto>();
        transaction.Should().NotBeNull();
        transaction!.Id.Should().Be(createdResult.Transaction.Id);
    }

    [Fact]
    public async Task GetTransaction_WhenNotExists_ShouldReturn404NotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/transactions/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
        public string? Reference { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
