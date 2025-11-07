using QuIXI.MQ.Serializers;
using IXICore.Meta;
using System.Collections.Concurrent;
using System.Text;

namespace QuIXI.MQ.Drivers
{
    public class MemoryQueue : MessageQueueBase
    {
        private readonly ConcurrentDictionary<string, List<Func<byte[], Task>>> _subscriptions = new();
        private readonly ConcurrentDictionary<string, List<byte[]>> _messages = new();

        public MemoryQueue(string name, IMessageSerializer serializer)
            : base(name, serializer) { }

        public override Task ConnectAsync()
        {
            Logging.info($"{Name} connected.");
            return Task.CompletedTask;
        }

        public override Task DisconnectAsync()
        {
            Logging.info($"{Name} disconnected.");
            _subscriptions.Clear();
            _messages.Clear();
            return Task.CompletedTask;
        }

        protected override Task PublishRawAsync(string topic, byte[] data)
        {
            Logging.trace($"Publishing to '{topic}': {UTF8Encoding.UTF8.GetString(data)}");
            var list = _messages.GetOrAdd(topic, _ => new List<byte[]>());
            list.Add(data);

            if (_subscriptions.TryGetValue(topic, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    _ = handler(data);
                }
            }

            return Task.CompletedTask;
        }

        protected override Task SubscribeRawAsync(string topic, Func<byte[], Task> onRawMessage,
            ReplayPosition replayPosition, int? countOrIndex)
        {
            var handlers = _subscriptions.GetOrAdd(topic, _ => new List<Func<byte[], Task>>());
            handlers.Add(onRawMessage);

            if (_messages.TryGetValue(topic, out var history))
            {
                IEnumerable<byte[]> replayData = replayPosition switch
                {
                    ReplayPosition.FromBeginning => history,
                    ReplayPosition.FromLast => history.Count > 0 ? new[] { history[^1] } : Enumerable.Empty<byte[]>(),
                    ReplayPosition.FromLastN => history.Skip(Math.Max(0, history.Count - (countOrIndex ?? 1))),
                    ReplayPosition.FromIndex => history.Skip(Math.Clamp(countOrIndex ?? 0, 0, history.Count)),
                    _ => Enumerable.Empty<byte[]>()
                };

                foreach (var msg in replayData)
                {
                    _ = onRawMessage(msg);
                }
            }

            return Task.CompletedTask;
        }

        protected override Task UnsubscribeRawAsync(string topic, Func<byte[], Task> onRawMessage)
        {
            if (_subscriptions.TryGetValue(topic, out var handlers))
            {
                handlers.Remove(onRawMessage);
                if (handlers.Count == 0)
                {
                    _subscriptions.TryRemove(topic, out _);
                }
            }
            return Task.CompletedTask;
        }

        public override ValueTask DisposeAsync() => new(DisconnectAsync());
    }
}
