using QuIXI.MQ.Serializers;
using IXICore.Meta;
using MQTTnet;
using System.Buffers;
using System.Collections.Concurrent;

namespace QuIXI.MQ.Drivers
{
    public class MqttQueue : MessageQueueBase
    {
        private readonly IMqttClient _client;
        private readonly MqttClientOptions _options;
        private readonly ConcurrentDictionary<string, List<Func<byte[], Task>>> _handlerMap = new();

        public MqttQueue(string name, IMessageSerializer serializer, string broker = "localhost", int port = 1883)
            : base(name, serializer)
        {
            var factory = new MqttClientFactory();
            _client = factory.CreateMqttClient();
            _options = new MqttClientOptionsBuilder()
                .WithClientId(name)
                .WithTcpServer(broker, port)
                .WithCleanSession()
                .Build();

            _client.ApplicationMessageReceivedAsync += OnMqttMessage;
        }

        public override async Task ConnectAsync()
        {
            if (!_client.IsConnected)
            {
                await _client.ConnectAsync(_options);
            }
        }

        public override async Task DisconnectAsync()
        {
            if (_client.IsConnected)
            {
                await _client.DisconnectAsync();
            }
        }

        protected override async Task PublishRawAsync(string topic, byte[] data)
        {
            var msg = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(data)
                .Build();


            await ConnectAsync();

            await _client.PublishAsync(msg);
        }

        private async Task OnMqttMessage(MqttApplicationMessageReceivedEventArgs ev)
        {
            var topic = ev.ApplicationMessage.Topic;
            var payload = ev.ApplicationMessage.Payload.ToArray();

            if (_handlerMap.TryGetValue(topic, out var handlers))
            {
                foreach (var handler in handlers.ToArray())
                {
                    try
                    {
                        await handler(payload);
                    }
                    catch (Exception ex)
                    {
                        Logging.error("Exception in MqttQueue.SubscribeRawAsync: " + ex);
                    }
                }
            }
        }

        protected override Task SubscribeRawAsync(string topic, Func<byte[], Task> onRawMessage,
            ReplayPosition replayPosition, int? countOrIndex)
        {
            RegisterHandler(topic, onRawMessage);
            return _client.SubscribeAsync(topic);
        }

        protected override Task UnsubscribeRawAsync(string topic, Func<byte[], Task> onRawMessage)
        {
            DeregisterHandler(topic, onRawMessage);

            if (!_handlerMap.TryGetValue(topic, out var list) || list.Count == 0)
            {
                _handlerMap.TryRemove(topic, out _);
                return _client.UnsubscribeAsync(topic);
            }

            return Task.CompletedTask;
        }

        private void RegisterHandler(string topic, Func<byte[], Task> handler)
        {
            var list = _handlerMap.GetOrAdd(topic, _ => new());
            lock (list)
            {
                list.Add(handler);
            }
        }

        private void DeregisterHandler(string topic, Func<byte[], Task> handler)
        {
            if (_handlerMap.TryGetValue(topic, out var list))
            {
                lock (list)
                {
                    list.Remove(handler);
                }
            }
        }

        public override ValueTask DisposeAsync() => new(DisconnectAsync());
    }
}
