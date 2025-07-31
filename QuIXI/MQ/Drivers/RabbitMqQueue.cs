using QuIXI.MQ.Serializers;
using IXICore.Meta;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;

namespace QuIXI.MQ.Drivers
{
    public class RabbitMqQueue : MessageQueueBase
    {
        private readonly string _exchangeName;
        private readonly ConnectionFactory _connFactory;
        private IConnection? _conn;
        private IChannel? _channel;
        private readonly ConcurrentDictionary<string, string> _consumerTags = new();

        public RabbitMqQueue(string name, IMessageSerializer serializer, string broker = "localhost", int port = 5672, string exchangeName = "app_exchange")
            : base(name, serializer)
        {
            _exchangeName = exchangeName;
            _connFactory = new ConnectionFactory
            {
                HostName = broker,
                Port = port
            };
        }

        public override async Task ConnectAsync()
        {
            _conn = await _connFactory.CreateConnectionAsync();
            _channel = await _conn.CreateChannelAsync();
            await _channel.ExchangeDeclareAsync(_exchangeName, ExchangeType.Fanout);
        }

        public override async Task DisconnectAsync()
        {
            if (_channel != null)
            {
                foreach (var kv in _consumerTags)
                {
                    await _channel.BasicCancelAsync(kv.Value);
                }

                _consumerTags.Clear();

                await _channel.CloseAsync();
                _channel.Dispose();
                _channel = null;
            }

            if (_conn != null)
            {
                await _conn.CloseAsync();
                _conn.Dispose();
                _conn = null;
            }
        }

        protected override async Task PublishRawAsync(string topic, byte[] data)
        {
            if (_channel == null)
                throw new InvalidOperationException("Channel not initialized. Call ConnectAsync first.");

            await _channel.BasicPublishAsync(_exchangeName, topic, body: data);
        }

        protected override async Task SubscribeRawAsync(
            string topic,
            Func<byte[], Task> onRawMessage,
            ReplayPosition replayPosition,
            int? countOrIndex)
        {
            if (_channel == null)
                throw new InvalidOperationException("Channel not initialized. Call ConnectAsync first.");

            var queue = await _channel.QueueDeclareAsync();
            await _channel.QueueBindAsync(queue.QueueName, _exchangeName, routingKey: "");

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    await onRawMessage(body);
                }
                catch (Exception e)
                {
                    Logging.error("Exception in RabbitMqQueue.SubscribeRawAsync: " + e);
                }
            };

            var consumerTag = await _channel.BasicConsumeAsync(queue.QueueName, autoAck: true, consumer);
            _consumerTags[topic] = consumerTag;
        }

        protected override async Task UnsubscribeRawAsync(string topic, Func<byte[], Task> onRawMessage)
        {
            if (_channel == null) return;

            if (_consumerTags.TryRemove(topic, out var tag))
            {
                await _channel.BasicCancelAsync(tag);
            }
        }

        public override ValueTask DisposeAsync() => new(DisconnectAsync());
    }
}
