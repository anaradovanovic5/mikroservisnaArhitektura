using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;

namespace EventService
{
    public interface ISagaEventPublisher
    {
        Task PublishAsync(string routingKey, string payload, CancellationToken cancellationToken);
    }

    public sealed class SagaEventPublisher : ISagaEventPublisher, IAsyncDisposable
    {
        private readonly ConnectionFactory _factory;
        private readonly RabbitMqOptions _options;
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public SagaEventPublisher(IOptions<RabbitMqOptions> options)
        {
            _options = options.Value;
            _factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password
            };
        }

        public async Task PublishAsync(string routingKey, string payload, CancellationToken cancellationToken)
        {
            await EnsureInitAsync(cancellationToken);
            var body = Encoding.UTF8.GetBytes(payload);
            await _channel!.BasicPublishAsync(
                exchange: "saga.koreografija",
                routingKey: routingKey,
                mandatory: false,
                basicProperties: new BasicProperties { Persistent = true, ContentType = "application/json" },
                body: body,
                cancellationToken: cancellationToken);
        }

        private async Task EnsureInitAsync(CancellationToken cancellationToken)
        {
            if (_channel is not null) return;
            await _lock.WaitAsync(cancellationToken);
            try
            {
                if (_channel is not null) return;
                _connection = await _factory.CreateConnectionAsync(cancellationToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
                await _channel.ExchangeDeclareAsync(
                    exchange: "saga.koreografija",
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false,
                    cancellationToken: cancellationToken);
            }
            finally { _lock.Release(); }
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel is not null) await _channel.DisposeAsync();
            if (_connection is not null) await _connection.DisposeAsync();
            _lock.Dispose();
        }
    }
}