using IXICore;
using IXICore.Storage;
using IXICore.Streaming;

namespace QuIXI.Meta
{
    internal class ICLocalStorageCallbacks : LocalStorageCallbacks
    {
        public bool receivedNewTransaction(Transaction transaction)
        {
            return Node.tiv.receivedNewTransaction(transaction);
        }

        public void processMessage(FriendMessage friendMessage)
        {
        }
    }
}
