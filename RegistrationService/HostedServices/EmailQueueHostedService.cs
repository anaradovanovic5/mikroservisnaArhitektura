using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.RateLimiting;

namespace RegistrationService.HostedServices
{
    public class EmailQueueHostedService : BackgroundService
    {
        private readonly RabbitMqOptions _options;
        private IConnection? _connection;
        private IChannel? _channel;

        // Rate limiter: max 2 emaila u sekundi
        private readonly RateLimiter _rateLimiter = new FixedWindowRateLimiter(
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 2,
                Window = TimeSpan.FromSeconds(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 100
            });

        public EmailQueueHostedService(IOptions<RabbitMqOptions> options)
        {
            _options = options.Value;
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

            await _channel.QueueDeclareAsync(
                queue: "dogadjaji.email",
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            // PrefetchCount = 1 da ne uzimamo više poruka nego što možemo da obradimo
            await _channel.BasicQosAsync(0, 1, false, stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                if (_channel is null) return;

                // Čekaj dozvolu od rate limitera
                using var lease = await _rateLimiter.AcquireAsync(1, stoppingToken);
                if (!lease.IsAcquired)
                {
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true);
                    return;
                }

                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                // Simulacija slanja emaila
                Console.WriteLine($"[Email] Šaljem email: {body}");
                await Task.Delay(100, stoppingToken); // simulacija

                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            };

            await _channel.BasicConsumeAsync("dogadjaji.email", autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

            Console.WriteLine("EmailQueueHostedService: sluša dogadjaji.email (rate limit: 2/s).");
            try { await Task.Delay(Timeout.Infinite, stoppingToken); }
            catch (OperationCanceledException) { }
        }

        public override void Dispose()
        {
            _rateLimiter.Dispose();
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}