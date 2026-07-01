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
    public class SagaKoreografijaConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SagaKoreografijaConsumer> _logger;
        private readonly RabbitMqOptions _options;
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

            await _channel.ExchangeDeclareAsync("saga.koreografija", ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: stoppingToken);

            var queue = await _channel.QueueDeclareAsync("saga.registrationservice", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
            // Sluša uspešnu rezervaciju lokacije — tek onda kreira prijavu
            await _channel.QueueBindAsync(queue.QueueName, "saga.koreografija", "lokacija.rezervisana", cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                _logger.LogInformation("[Saga Koreografija][RegistrationService] Primio: lokacija.rezervisana");

                try
                {
                    var ev = JsonSerializer.Deserialize<LokacijaRezervacisanaEvent>(body)!;

                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<RegistrationDbContext>();

                    var vecPostoji = await db.Prijave.AnyAsync(p => p.DogadjajId == ev.DogadjajId, stoppingToken);

                    if (!vecPostoji)
                    {
                        db.Prijave.Add(new Prijava
                        {
                            ImeUcesnika = "Saga",
                            PrezimeUcesnika = "Automatska",
                            EmailUcesnika = $"saga-{ev.SagaId}@dogadjajiapp.local",
                            DatumPrijave = DateTime.UtcNow,
                            DogadjajId = ev.DogadjajId
                        });

                        await db.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("[Saga Koreografija][RegistrationService] Prijava upisana u bazu za DogadjajId={Id}, SagaId={SagaId}", ev.DogadjajId, ev.SagaId);
                    }
                    else
                    {
                        _logger.LogInformation("[Saga Koreografija][RegistrationService] Prijava za DogadjajId={Id} već postoji — preskačem upis, SagaId={SagaId}", ev.DogadjajId, ev.SagaId);
                    }

                    var okEv = JsonSerializer.Serialize(new PrijavaKreiranaEvent
                    {
                        SagaId = ev.SagaId,
                        DogadjajId = ev.DogadjajId
                    });
                    await PublishAsync("prijava.kreirana", okEv, stoppingToken);

                    await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Saga Koreografija][RegistrationService] Greska");

                    // Pokušaj deserijalizacije da bismo imali SagaId i LokacijaId za kompenzaciju
                    try
                    {
                        var ev = JsonSerializer.Deserialize<LokacijaRezervacisanaEvent>(body)!;
                        var failEv = JsonSerializer.Serialize(new PrijavaNeuspesnaEvent
                        {
                            SagaId = ev.SagaId,
                            DogadjajId = ev.DogadjajId,
                            LokacijaId = ev.LokacijaId,
                            Razlog = ex.Message
                        });
                        await PublishAsync("prijava.neuspesna", failEv, stoppingToken);
                    }
                    catch { /* ako ni ovo ne uspe, nema sta */ }

                    await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, cancellationToken: stoppingToken);
                }
            };

            await _channel.BasicConsumeAsync(queue.QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
            _logger.LogInformation("[Saga Koreografija] RegistrationService consumer pokrenut.");
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
