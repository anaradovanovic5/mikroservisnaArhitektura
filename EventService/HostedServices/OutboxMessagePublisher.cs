using EventService.Data;
using Microsoft.EntityFrameworkCore;

namespace EventService.HostedServices
{
    public class OutboxMessagePublisher : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxMessagePublisher> _logger;

        public OutboxMessagePublisher(IServiceScopeFactory scopeFactory, ILogger<OutboxMessagePublisher> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();
                    var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();

                    var pending = await db.OutboxMessages
                        .OrderBy(x => x.CreatedAt)
                        .Take(5)
                        .ToListAsync(stoppingToken);

                    foreach (var msg in pending)
                    {
                        try
                        {
                            await publisher.PublishAsync(msg.Payload, msg.Id.ToString(), msg.EventType, stoppingToken);
                            db.OutboxMessages.Remove(msg);
                            await db.SaveChangesAsync(stoppingToken);
                            Console.WriteLine($"Outbox: poruka {msg.Id} poslata na RabbitMQ.");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Nije uspelo slanje outbox poruke {Id}", msg.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Greška u OutboxMessagePublisher.");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
