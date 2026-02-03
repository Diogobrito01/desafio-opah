using System.Text;
using System.Text.Json;
using CashFlow.BuildingBlocks.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;

namespace CashFlow.BuildingBlocks.EventBus.RabbitMQ;

/// <summary>
/// RabbitMQ implementation of the event bus with retry policy for resilience
/// </summary>
public class RabbitMqEventBus : IEventBus, IDisposable
{
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly RabbitMqSettings _settings;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly AsyncRetryPolicy _retryPolicy;
    private bool _disposed;

    public RabbitMqEventBus(
        ILogger<RabbitMqEventBus> logger,
        IOptions<RabbitMqSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;

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

        // Declare exchange for pub/sub pattern
        _channel.ExchangeDeclare(
            exchange: _settings.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        // Retry policy for transient failures
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

        _logger.LogInformation("RabbitMQ connection established to {Host}:{Port}", _settings.Host, _settings.Port);
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        var eventName = @event.GetType().Name;
        var routingKey = eventName;

        _logger.LogInformation("Publishing event {EventName} with Id {EventId}", eventName, @event.EventId);

        await _retryPolicy.ExecuteAsync(async () =>
        {
            var body = SerializeEvent(@event);

            var properties = _channel.CreateBasicProperties();
            properties.DeliveryMode = 2; // Persistent
            properties.ContentType = "application/json";
            properties.MessageId = @event.EventId.ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            _channel.BasicPublish(
                exchange: _settings.ExchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Event {EventName} published successfully", eventName);

            await Task.CompletedTask;
        });
    }

    private static byte[] SerializeEvent<TEvent>(TEvent @event)
    {
        var json = JsonSerializer.Serialize(@event, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        return Encoding.UTF8.GetBytes(json);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _logger.LogInformation("Disposing RabbitMQ connection");

        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
