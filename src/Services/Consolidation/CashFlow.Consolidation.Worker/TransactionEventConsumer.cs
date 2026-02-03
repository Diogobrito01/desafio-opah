using System.Text;
using System.Text.Json;
using CashFlow.BuildingBlocks.EventBus.RabbitMQ;
using CashFlow.Consolidation.Application.Commands.ProcessTransaction;
using MediatR;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CashFlow.Consolidation.Worker;

/// <summary>
/// Background service that consumes transaction events from RabbitMQ
/// Implements resilience with retry policy and handles up to 50 req/s as per requirements
/// </summary>
public sealed class TransactionEventConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TransactionEventConsumer> _logger;
    private readonly RabbitMqSettings _settings;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly AsyncRetryPolicy _retryPolicy;

    public TransactionEventConsumer(
        IServiceProvider serviceProvider,
        ILogger<TransactionEventConsumer> logger,
        IOptions<RabbitMqSettings> settings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings.Value;

        // Retry policy for processing failures
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Retry {RetryCount} after {TimeSpan}s due to {ExceptionMessage}",
                        retryCount,
                        timeSpan.TotalSeconds,
                        exception.Message);
                });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield(); // Ensure the method is async

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                InitializeRabbitMQ();
                
                _logger.LogInformation("Transaction event consumer started");

                // Keep the worker running
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Transaction event consumer is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in transaction event consumer. Retrying in 10 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private void InitializeRabbitMQ()
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            Port = _settings.Port,
            UserName = _settings.Username,
            Password = _settings.Password,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare exchange
        _channel.ExchangeDeclare(
            exchange: _settings.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        // Declare queue
        var queueName = "cashflow-consolidation-queue";
        _channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        // Bind queue to exchange
        _channel.QueueBind(
            queue: queueName,
            exchange: _settings.ExchangeName,
            routingKey: "TransactionCreatedIntegrationEvent");

        // Set prefetch count to handle 50 req/s
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            await ProcessMessageAsync(ea);
        };

        _channel.BasicConsume(
            queue: queueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("Connected to RabbitMQ and listening for transaction events");
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea)
    {
        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            _logger.LogInformation("Received transaction event: {Message}", message);

            var @event = JsonSerializer.Deserialize<TransactionCreatedIntegrationEvent>(message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (@event is null)
            {
                _logger.LogWarning("Failed to deserialize transaction event");
                _channel?.BasicNack(ea.DeliveryTag, false, false);
                return;
            }

            await _retryPolicy.ExecuteAsync(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var command = new ProcessTransactionCommand
                {
                    TransactionId = @event.TransactionId,
                    Amount = @event.Amount,
                    Type = @event.Type,
                    TransactionDate = @event.TransactionDate
                };

                var result = await mediator.Send(command);

                if (result.IsFailure)
                {
                    _logger.LogError("Failed to process transaction {TransactionId}: {Error}",
                        @event.TransactionId,
                        result.Error.Message);
                    throw new InvalidOperationException(result.Error.Message);
                }

                _logger.LogInformation("Transaction {TransactionId} processed successfully", @event.TransactionId);
            });

            _channel?.BasicAck(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transaction event");
            
            // Reject and requeue with a limit (5% loss tolerance as per requirements)
            // In production, implement dead-letter queue for persistent failures
            _channel?.BasicNack(ea.DeliveryTag, false, requeue: false);
        }
    }

    public override void Dispose()
    {
        _logger.LogInformation("Disposing transaction event consumer");
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }

    private sealed class TransactionCreatedIntegrationEvent
    {
        public Guid EventId { get; set; }
        public DateTime OccurredOn { get; set; }
        public Guid TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
    }
}
