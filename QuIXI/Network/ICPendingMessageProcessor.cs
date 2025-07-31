
using QuIXI.Meta;
using QuIXI.MQ;
using IXICore;
using IXICore.Streaming;

namespace QuIXI.Network
{
    internal class ICPendingMessageProcessor : PendingMessageProcessor
    {
        public ICPendingMessageProcessor(string root_storage_path, bool enable_push_notification_server) : base(root_storage_path, enable_push_notification_server)
        {
        }

        protected override void onMessageSent(Friend friend, int channel, StreamMessage msg)
        {
            Node.messageQueue.PublishAsync(MQTopics.MessageSent, msg);
        }
    }
}
