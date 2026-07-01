using EventService.Data;
using EventService.Domains;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Events;
using System.Text;
using System.Text.Json;

namespace EventService.HostedServices
{
    public class SagaKoreografijaConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly RabbitMqOptions _options;
        private readonly ILogger<SagaKoreografijaConsumer> _logger;
        private IConnection? _connection;
        private IChannel? _channel;

        public SagaKoreografijaConsumer(IServiceScopeFactory scopeFactory, IOptions<RabbitMqOptions> options, ILogger<SagaKoreografijaConsumer> logger)
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
                exchange: "saga.koreografija",
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

            var queue = await _channel.QueueDeclareAsync(
                queue: "saga.eventservice.kompenzacija",
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await _channel.QueueBindAsync(queue.QueueName, "saga.koreografija", "lokacija.rezervacija.neuspesna", cancellationToken: stoppingToken);
            await _channel.QueueBindAsync(queue.QueueName, "saga.koreografija", "prijava.neuspesna", cancellationToken: stoppingToken);
            await _channel.QueueBindAsync(queue.QueueName, "saga.koreografija", "prijava.kreirana", cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                var eventType = ea.RoutingKey;
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                _logger.LogInformation("[Saga Koreografija][EventService] Primio: {EventType}", eventType);

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();

                    if (eventType == "lokacija.rezervacija.neuspesna")
                    {
                        var ev = JsonSerializer.Deserialize<LokacijaRezervacijaNeuspesnaEvent>(body)!;

                        var saga = await db.SagaStates.FirstOrDefaultAsync(s => s.SagaId == ev.SagaId, stoppingToken);
                        if (saga == null || saga.Status != "Started")
                        {
                            _logger.LogWarning("[Saga Koreografija][Gate] Ignorišem {EventType} za SagaId={SagaId} — trenutni status='{Status}' (očekivano 'Started'), verovatno duplikat ili poruka van reda.",
                                eventType, ev.SagaId, saga?.Status ?? "NE POSTOJI");
                            await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                            return;
                        }

                        var dogadjaj = await db.Dogadjaji.FindAsync(ev.DogadjajId);
                        if (dogadjaj != null)
                        {
                            db.Dogadjaji.Remove(dogadjaj);
                        }

                        saga.Status = "Failed";
                        saga.CurrentStep = eventType;
                        saga.ErrorMessage = ev.Razlog;
                        saga.UpdatedAt = DateTime.UtcNow;
                        await db.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation("[Saga Koreografija][Kompenzacija] Dogadjaj {Id} obrisan (rezervacija lokacije pala, SagaId={SagaId})", ev.DogadjajId, ev.SagaId);
                    }
                    else if (eventType == "prijava.neuspesna")
                    {
                        var ev = JsonSerializer.Deserialize<PrijavaNeuspesnaEvent>(body)!;

                        var saga = await db.SagaStates.FirstOrDefaultAsync(s => s.SagaId == ev.SagaId, stoppingToken);
                        if (saga == null || saga.Status != "Started")
                        {
                            _logger.LogWarning("[Saga Koreografija][Gate] Ignorišem {EventType} za SagaId={SagaId} — trenutni status='{Status}' (očekivano 'Started'), verovatno duplikat ili poruka van reda.",
                                eventType, ev.SagaId, saga?.Status ?? "NE POSTOJI");
                            await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                            return;
                        }

                        var dogadjaj = await db.Dogadjaji.FindAsync(ev.DogadjajId);
                        if (dogadjaj != null)
                        {
                            db.Dogadjaji.Remove(dogadjaj);
                        }

                        saga.Status = "Failed";
                        saga.CurrentStep = eventType;
                        saga.ErrorMessage = ev.Razlog;
                        saga.UpdatedAt = DateTime.UtcNow;
                        await db.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation("[Saga Koreografija][Kompenzacija] Dogadjaj {Id} obrisan (prijava pala, SagaId={SagaId})", ev.DogadjajId, ev.SagaId);
                    }
                    else if (eventType == "prijava.kreirana")
                    {
                        var ev = JsonSerializer.Deserialize<PrijavaKreiranaEvent>(body)!;

                        var saga = await db.SagaStates.FirstOrDefaultAsync(s => s.SagaId == ev.SagaId, stoppingToken);
                        if (saga == null || saga.Status != "Started")
                        {
                            _logger.LogWarning("[Saga Koreografija][Gate] Ignorišem {EventType} za SagaId={SagaId} — trenutni status='{Status}' (očekivano 'Started'), verovatno duplikat ili poruka van reda.",
                                eventType, ev.SagaId, saga?.Status ?? "NE POSTOJI");
                            await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                            return;
                        }

                        saga.Status = "Completed";
                        saga.CurrentStep = eventType;
                        saga.UpdatedAt = DateTime.UtcNow;
                        await db.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation("[Saga Koreografija] Saga {SagaId} uspešno završena (prijava kreirana, DogadjajId={DogadjajId}).", ev.SagaId, ev.DogadjajId);
                    }

                    await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Saga Koreografija][EventService] Greska pri obradi {EventType}", eventType);
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, cancellationToken: stoppingToken);
                }
            };

            await _channel.BasicConsumeAsync(queue.QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
            _logger.LogInformation("[Saga Koreografija] EventService consumer pokrenut.");

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