
namespace QuIXI.MQ
{
    public static class MQTopics
    {
        // Protocol Messages
        public static readonly string Chat = "Chat";
        //public static readonly string GetNick = "GetNick";
        public static readonly string Nick = "Nick";
        //public static readonly string RequestAdd = "RequestAdd";
        //public static readonly string AcceptAdd = "AcceptAdd";
        public static readonly string SentFunds = "SentFunds";
        public static readonly string RequestFunds = "RequestFunds";
        //public static readonly string Keys = "Keys";
        public static readonly string MsgRead = "MsgRead";
        public static readonly string MsgReceived = "MsgReceived";
        public static readonly string FileData = "FileData";
        public static readonly string RequestFileData = "RequestFileData";
        public static readonly string FileHeader = "FileHeader";
        public static readonly string AcceptFile = "AcceptFile";
        //public static readonly string RequestCall = "RequestCall";
        //public static readonly string AcceptCall = "AcceptCall";
        //public static readonly string RejectCall = "RejectCall";
        //public static readonly string CallData = "CallData";
        public static readonly string RequestFundsResponse = "RequestFundsResponse";
        public static readonly string AcceptAddBot = "AcceptAddBot";
        //public static readonly string BotGetMessages = "BotGetMessages";
        public static readonly string AppData = "AppData";
        public static readonly string AppRequest = "AppRequest";
        public static readonly string FileFullyReceived = "FileFullyReceived";
        public static readonly string Avatar = "Avatar";
        //public static readonly string GetAvatar = "GetAvatar";
        //public static readonly string GetPubKey = "GetPubKey";
        //public static readonly string PubKey = "PubKey";
        public static readonly string AppRequestAccept = "AppRequestAccept";
        public static readonly string AppRequestReject = "AppRequestReject";
        public static readonly string AppRequestError = "AppRequestError";
        public static readonly string AppEndSession = "AppEndSession";
        public static readonly string BotAction = "BotAction";
        public static readonly string MsgDelete = "MsgDelete";
        public static readonly string MsgReaction = "MsgReaction";
        public static readonly string MsgTyping = "MsgTyping";
        //public static readonly string MsgError = "MsgError";
        //public static readonly string Leave = "Leave";
        public static readonly string LeaveConfirmed = "LeaveConfirmed";
        //public static readonly string MsgReport = "MsgReport";
        public static readonly string RequestAdd2 = "RequestAdd2";
        public static readonly string AcceptAdd2 = "AcceptAdd2";
        //public static readonly string Keys2 = "Keys2";
        public static readonly string GetAppProtocols = "GetAppProtocols";
        public static readonly string AppProtocols = "AppProtocols";
        public static readonly string AppProtocolData = "AppProtocolData";

        // Misc Messages
        public static readonly string FriendStatusUpdate = "FriendStatusUpdate";
        public static readonly string MessageSent = "MessageSent";
        public static readonly string MessageExpired = "MessageExpired";
        public static readonly string TransactionStatusUpdate = "TransactionStatusUpdate";
        

        // Raw out
    }

    public interface IMessageQueue : IAsyncDisposable
    {
        string Name { get; }

        Task ConnectAsync();
        Task DisconnectAsync();

        Task PublishAsync<T>(string topic, T message);

        Task SubscribeAsync<T>(string topic, Func<T, Task> onMessage,
            ReplayPosition replayPosition = ReplayPosition.FromLast, int? countOrIndex = null);

        Task UnsubscribeAsync<T>(string topic, Func<T, Task> onMessage);
    }
}
