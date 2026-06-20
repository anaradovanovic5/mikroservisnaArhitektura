using EventService;
using EventService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;

namespace EventService
{
    public interface IRabbitMqRequestReplyClient
    {
        Task<string?> SendRequestAsync(string payload, string routingKey, CancellationToken cancellationToken);
    }

    public sealed class RabbitMqRequestReplyClient : IRabbitMqRequestReplyClient, IAsyncDisposable
    {
        private readonly ConnectionFactory _factory;
        private readonly RabbitMqOptions _options;
        private IConnection? _connection;
        private IChannel? _channel;
        private string? _replyQueueName;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingRequests = new();

        public RabbitMqRequestReplyClient(IOptions<RabbitMqOptions> options)
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

        public async Task InitAsync(CancellationToken cancellationToken)
        {
            _connection = await _factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            // Ekskluzivni reply red koji RabbitMQ sam imenuje
            var replyQueue = await _channel.QueueDeclareAsync(
                queue: "",
                durable: false,
                exclusive: true,
                autoDelete: true,
                cancellationToken: cancellationToken);
            _replyQueueName = replyQueue.QueueName;

            // Request red u koji RegistrationService sluša
            await _channel.QueueDeclareAsync(
                queue: "dogadjaji.request",
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken);

            // Consumer koji prima odgovore
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += (_, ea) =>
            {
                var corrId = ea.BasicProperties.CorrelationId;
                if (corrId != null && _pendingRequests.TryRemove(corrId, out var tcs))
                {
                    var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                    tcs.SetResult(body);
                }
                return Task.CompletedTask;
            };

            await _channel.BasicConsumeAsync(
                queue: _replyQueueName,
                autoAck: true,
                consumer: consumer,
                cancellationToken: cancellationToken);
        }

        public async Task<string?> SendRequestAsync(string payload, string routingKey, CancellationToken cancellationToken)
        {
            if (_channel is null) await InitAsync(cancellationToken);

            var correlationId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<string>();
            _pendingRequests[correlationId] = tcs;

            var props = new BasicProperties
            {
                CorrelationId = correlationId,
                ReplyTo = _replyQueueName,
                ContentType = "application/json"
            };

            var body = Encoding.UTF8.GetBytes(payload);
            await _channel!.BasicPublishAsync(
                exchange: "",
                routingKey: routingKey,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: cancellationToken);

            Console.WriteLine($"[Request-Reply] Zahtev poslat, CorrelationId={correlationId}");

            // Čekaj odgovor max 10 sekundi
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            cts.Token.Register(() => tcs.TrySetCanceled());

            try
            {
                return await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                _pendingRequests.TryRemove(correlationId, out _);
                Console.WriteLine($"[Request-Reply] Timeout za CorrelationId={correlationId}");
                return null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel is not null) await _channel.DisposeAsync();
            if (_connection is not null) await _connection.DisposeAsync();
        }
    }
}