using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EventService.HostedServices
{
    public class RequestReplyHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly RabbitMqOptions _options;
        private IConnection? _connection;
        private IChannel? _channel;

        public RequestReplyHostedService(IServiceScopeFactory scopeFactory, IOptions<RabbitMqOptions> options)
        {
            _scopeFactory = scopeFactory;
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
                queue: "dogadjaji.request",
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                if (_channel is null) return;

                try
                {
                    var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var request = JsonSerializer.Deserialize<Dictionary<string, int>>(body);
                    int dogadjajId = request?["DogadjajId"] ?? 0;

                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<RegistrationDbContext>();

                    var brojPrijava = db.Prijave.Count(p => p.DogadjajId == dogadjajId);
                    var odgovor = JsonSerializer.Serialize(new { DogadjajId = dogadjajId, BrojPrijava = brojPrijava });

                    Console.WriteLine($"[Request-Reply] Zahtev za DogadjajId={dogadjajId}, šaljem odgovor: {odgovor}");

                    // Pošalji odgovor na replyTo red sa istim CorrelationId
                    var replyProps = new BasicProperties
                    {
                        CorrelationId = ea.BasicProperties.CorrelationId
                    };
                    await _channel.BasicPublishAsync(
                        exchange: "",
                        routingKey: ea.BasicProperties.ReplyTo!,
                        mandatory: false,
                        basicProperties: replyProps,
                        body: Encoding.UTF8.GetBytes(odgovor));

                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Request-Reply] Greška: {ex.Message}");
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: "dogadjaji.request",
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            Console.WriteLine("RegistrationService: sluša Request-Reply red.");
            try { await Task.Delay(Timeout.Infinite, stoppingToken); }
            catch (OperationCanceledException) { }
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
