using LocationService.Data;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Events;
using System.Text;
using System.Text.Json;

namespace LocationService.HostedServices
{
    public class SagaKoreografijaConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SagaKoreografijaConsumer> _logger;
        private readonly string _hostName;
        private IConnection? _connection;
        private IChannel? _channel;

        public SagaKoreografijaConsumer(IServiceScopeFactory scopeFactory, ILogger<SagaKoreografijaConsumer> logger, IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _hostName = config["RabbitMq:HostName"] ?? "localhost";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory { HostName = _hostName };
            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await _channel.ExchangeDeclareAsync("saga.koreografija", ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: stoppingToken);

            var queue = await _channel.QueueDeclareAsync("saga.locationservice", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
            await _channel.QueueBindAsync(queue.QueueName, "saga.koreografija", "dogadjaj.kreiran", cancellationToken: stoppingToken);
            await _channel.QueueBindAsync(queue.QueueName, "saga.koreografija", "prijava.neuspesna", cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var eventType = ea.RoutingKey;
                _logger.LogInformation("[Saga Koreografija][LocationService] Primio: {EventType}", eventType);

                try
                {
                    if (eventType == "dogadjaj.kreiran")
                    {
                        var ev = JsonSerializer.Deserialize<DogadjajKreiranSagaEvent>(body)!;

                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<LocationDbContext>();
                        var lokacija = await db.Lokacije.FindAsync(ev.LokacijaId);

                        if (lokacija == null)
                        {
                            // Kompenzacija — lokacija ne postoji
                            var failEv = JsonSerializer.Serialize(new LokacijaRezervacijaNeuspesnaEvent
                            {
                                SagaId = ev.SagaId,
                                DogadjajId = ev.DogadjajId,
                                Razlog = $"Lokacija {ev.LokacijaId} ne postoji."
                            });
                            await PublishAsync("lokacija.rezervacija.neuspesna", failEv, stoppingToken);
                            _logger.LogWarning("[Saga Koreografija][LocationService] Lokacija {Id} ne postoji — objavljen neuspeh.", ev.LokacijaId);
                        }
                        else
                        {
                            // Uspeh — rezervacija OK
                            var okEv = JsonSerializer.Serialize(new LokacijaRezervacisanaEvent
                            {
                                SagaId = ev.SagaId,
                                DogadjajId = ev.DogadjajId,
                                LokacijaId = ev.LokacijaId
                            });
                            await PublishAsync("lokacija.rezervisana", okEv, stoppingToken);
                            _logger.LogInformation("[Saga Koreografija][LocationService] Lokacija {Id} rezervisana, SagaId={SagaId}", ev.LokacijaId, ev.SagaId);
                        }
                    }
                    else if (eventType == "prijava.neuspesna")
                    {
                        // Kompenzacija — oslobodi rezervaciju
                        var ev = JsonSerializer.Deserialize<PrijavaNeuspesnaEvent>(body)!;
                        _logger.LogInformation("[Saga Koreografija][Kompenzacija] Rezervacija lokacije {Id} oslobodjena, SagaId={SagaId}", ev.LokacijaId, ev.SagaId);
                    }

                    await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Saga Koreografija][LocationService] Greska");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, cancellationToken: stoppingToken);
                }
            };

            await _channel.BasicConsumeAsync(queue.QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
            _logger.LogInformation("[Saga Koreografija] LocationService consumer pokrenut.");
            try { await Task.Delay(Timeout.Infinite, stoppingToken); }
            catch (OperationCanceledException) { }
        }

        private async Task PublishAsync(string routingKey, string payload, CancellationToken ct)
        {
            var body = Encoding.UTF8.GetBytes(payload);
            await _channel!.BasicPublishAsync(
                exchange: "saga.koreografija",
                routingKey: routingKey,
                mandatory: false,
                basicProperties: new BasicProperties { Persistent = true },
                body: body,
                cancellationToken: ct);
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}