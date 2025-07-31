using QuIXI.MQ.Serializers;
using System.Collections.Concurrent;

namespace QuIXI.MQ
{
    public abstract class MessageQueueBase : IMessageQueue
    {
        public string Name { get; }
        protected IMessageSerializer Serializer { get; }
        protected MessageQueueBase(string name, IMessageSerializer serializer)
        {
            Name = name;
            Serializer = serializer;
        }

        public abstract Task ConnectAsync();
        public abstract Task DisconnectAsync();
        public abstract ValueTask DisposeAsync();

        public Task PublishAsync<T>(string topic, T message)
        {
            var rawData = Serializer.Serialize(message);
            return PublishRawAsync(topic, rawData);
        }

        protected abstract Task PublishRawAsync(string topic, byte[] data);

        private readonly ConcurrentDictionary<(string, object), Func<byte[], Task>> _rawHandlerMap = new();

        public async Task SubscribeAsync<T>(string topic, Func<T, Task> onMessage,
            ReplayPosition replayPosition = ReplayPosition.FromLast, int? countOrIndex = null)
        {
            Func<byte[], Task> rawHandler = async rawData =>
            {
                var msg = Serializer.Deserialize<T>(rawData);
                await onMessage(msg);
            };

            _rawHandlerMap[(topic, onMessage)] = rawHandler;

            await SubscribeRawAsync(topic, rawHandler, replayPosition, countOrIndex);
        }

        protected abstract Task SubscribeRawAsync(string topic, Func<byte[], Task> onRawMessage,
            ReplayPosition replayPosition, int? countOrIndex);

        public async Task UnsubscribeAsync<T>(string topic, Func<T, Task> onMessage)
        {
            if (_rawHandlerMap.TryRemove((topic, onMessage), out var rawHandler))
            {
                await UnsubscribeRawAsync(topic, rawHandler);
            }
        }

        protected abstract Task UnsubscribeRawAsync(string topic, Func<byte[], Task> onRawMessage);
    }
}
