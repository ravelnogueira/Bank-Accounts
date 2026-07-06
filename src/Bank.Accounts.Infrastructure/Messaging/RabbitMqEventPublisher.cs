using System.Text;
using RabbitMQ.Client;
using Microsoft.Extensions.Options;
using Bank.Accounts.Application.Outbox.Interfaces;

namespace Bank.Accounts.Infrastructure.Messaging;

public sealed class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private static readonly TimeSpan PublishConfirmTimeout = TimeSpan.FromSeconds(5);
    private readonly RabbitMqOptions _options;
    private readonly object _connectionLock = new();
    private IConnection? _connection;

    public RabbitMqEventPublisher(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public Task PublishAsync(Guid messageId, string eventType, string payload, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var channel = GetConnection().CreateModel();
        channel.ConfirmSelect();
        channel.ExchangeDeclare(_options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = messageId.ToString();
        properties.Type = eventType;
        properties.ContentType = "application/json";

        channel.BasicPublish(_options.Exchange, eventType, mandatory: false, properties, Encoding.UTF8.GetBytes(payload));
        channel.WaitForConfirmsOrDie(PublishConfirmTimeout);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }

    private IConnection GetConnection()
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        lock (_connectionLock)
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            _connection?.Dispose();
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                AutomaticRecoveryEnabled = true,
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection("bank-accounts-outbox");
            return _connection;
        }
    }
}
