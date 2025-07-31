using QuIXI.MQ.Serializers;

namespace QuIXI.MQ.Drivers
{
    public class DummyQueue : MessageQueueBase
    {
        public DummyQueue(string name, IMessageSerializer serializer)
            : base(name, serializer) { }

        public override Task ConnectAsync()
        {
            return Task.CompletedTask;
        }

        public override Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }

        protected override Task PublishRawAsync(string topic, byte[] data)
        {
            return Task.CompletedTask;
        }

        protected override Task SubscribeRawAsync(string topic, Func<byte[], Task> onRawMessage,
            ReplayPosition replayPosition, int? countOrIndex)
        {
            return Task.CompletedTask;
        }

        protected override Task UnsubscribeRawAsync(string topic, Func<byte[], Task> onRawMessage)
        {
            return Task.CompletedTask;
        }

        public override ValueTask DisposeAsync() => new(DisconnectAsync());
    }
}
