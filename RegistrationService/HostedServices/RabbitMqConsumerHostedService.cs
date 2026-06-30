using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RegistrationService.Data;
using RegistrationService.Domains;
using Shared.Events;
using System.Text;
using System.Text.Json;

namespace RegistrationService.HostedServices
{
    public sealed class RabbitMqConsumerHostedService : BackgroundService
    {
        private const string RetryCountHeader = "x-retry-count";
        private const int MaxRetryAttempts = 10;
        private const string DlxExchange = "dogadjaji.dlx";
        private const string DlqQueue = "dogadjaji.dlq";
        private const string DlqRoutingKey = "dogadjaji.dlq";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly RabbitMqOptions _options;
        private readonly ILogger<RabbitMqConsumerHostedService> _logger;
        private IConnection? _connection;
        private IChannel? _channel;

        public RabbitMqConsumerHostedService(
            IServiceScopeFactory scopeFactory,
            IOptions<RabbitMqOptions> options,
            ILogger<RabbitMqConsumerHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password
            };

            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await _channel.ExchangeDeclareAsync(
                exchange: _options.Exchange,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                queue: _options.Queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            await _channel.QueueBindAsync(
                queue: _options.Queue,
                exchange: _options.Exchange,
                routingKey: _options.RoutingKey,
                cancellationToken: stoppingToken);

            // Finalni DLQ
            await _channel.ExchangeDeclareAsync(
                exchange: DlxExchange,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                queue: DlqQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            await _channel.QueueBindAsync(
                queue: DlqQueue,
                exchange: DlxExchange,
                routingKey: DlqRoutingKey,
                cancellationToken: stoppingToken);

            await _channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: _options.PrefetchCount,
                global: false,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) => await HandleMessageAsync(ea, stoppingToken);

            await _channel.BasicConsumeAsync(
                queue: _options.Queue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            Console.WriteLine("RegistrationService: sluša RabbitMQ red za DogadjajCreated.");

            try { await Task.Delay(Timeout.Infinite, stoppingToken); }
            catch (OperationCanceledException) { }
        }

        private async Task HandleMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            if (_channel is null) return;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<RegistrationDbContext>();

                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var ev = JsonSerializer.Deserialize<DogadjajCreatedEvent>(body);

                if (ev is null)
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                    return;
                }

                await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

                // IDEMPOTENT CONSUMER
                var alreadyProcessed = await db.ProcessedMessages
                    .AnyAsync(x => x.EventId == ea.BasicProperties.MessageId, cancellationToken);

                if (!alreadyProcessed)
                {
                    db.DogadjajReference.Add(new DogadjajReference
                    {
                        DogadjajId = ev.DogadjajId,
                        NazivDogadjaja = ev.NazivDogadjaja,
                        Datum = ev.Datum
                    });

                    db.ProcessedMessages.Add(new ProcessedMessage
                    {
                        EventId = ea.BasicProperties.MessageId!,
                        EventType = "DogadjajCreated",
                        ProcessedAtUtc = DateTime.UtcNow
                    });

                    await db.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);

                    Console.WriteLine($"Consumer: DogadjajReference kreiran za DogadjajId={ev.DogadjajId}");
                }
                else
                {
                    Console.WriteLine($"Consumer: poruka {ea.BasicProperties.MessageId} već obrađena — preskačem (idempotency).");
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Greška pri obradi RabbitMQ poruke.");
                await HandleFailureAsync(ea, cancellationToken);
            }
        }

        private async Task HandleFailureAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            if (_channel is null) return;

            var headers = ea.BasicProperties.Headers;
            long currentRetryCount = 0;
            if (headers is not null && headers.TryGetValue(RetryCountHeader, out var raw) && raw is not null)
            {
                currentRetryCount = raw switch
                {
                    long l => l,
                    int i => i,
                    byte[] b => long.Parse(Encoding.UTF8.GetString(b)),
                    _ => 0
                };
            }

            var nextRetryCount = currentRetryCount + 1;

            if (nextRetryCount < MaxRetryAttempts)
            {
                _logger.LogWarning(
                    "Pokušaj {Attempt}/{Max} nije uspeo za poruku {MessageId} — vraćam u red.",
                    nextRetryCount, MaxRetryAttempts, ea.BasicProperties.MessageId);

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

                var retryProperties = new BasicProperties
                {
                    Persistent = true,
                    MessageId = ea.BasicProperties.MessageId,
                    Type = ea.BasicProperties.Type,
                    ContentType = ea.BasicProperties.ContentType,
                    Headers = new Dictionary<string, object?>
                    {
                        { RetryCountHeader, nextRetryCount }
                    }
                };

                await _channel.BasicPublishAsync(
                    exchange: _options.Exchange,
                    routingKey: _options.RoutingKey,
                    mandatory: true,
                    basicProperties: retryProperties,
                    body: ea.Body.ToArray(),
                    cancellationToken: cancellationToken);
            }
            else
            {
                _logger.LogError(
                    "Poruka {MessageId} dostigla {Max} neuspešnih pokušaja — šaljem na finalni DLQ.",
                    ea.BasicProperties.MessageId, MaxRetryAttempts);

                var dlqProperties = new BasicProperties
                {
                    Persistent = true,
                    MessageId = ea.BasicProperties.MessageId,
                    Type = ea.BasicProperties.Type,
                    ContentType = ea.BasicProperties.ContentType,
                    Headers = new Dictionary<string, object?>
                    {
                        { RetryCountHeader, nextRetryCount }
                    }
                };

                await _channel.BasicPublishAsync(
                    exchange: DlxExchange,
                    routingKey: DlqRoutingKey,
                    mandatory: true,
                    basicProperties: dlqProperties,
                    body: ea.Body.ToArray(),
                    cancellationToken: cancellationToken);
            }
            await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}