using QuIXI.MQ;
using QuIXI.MQ.Drivers;
using QuIXI.MQ.Serializers;
using QuIXI.Network;
using IXICore;
using IXICore.Inventory;
using IXICore.Meta;
using IXICore.Network;
using IXICore.RegNames;
using IXICore.Storage;
using IXICore.Streaming;
using IXICore.Utils;
using static IXICore.Transaction;

namespace QuIXI.Meta
{
    class Node : IxianNode
    {
        public static TransactionInclusion tiv = null;

        public static StreamProcessor streamProcessor = null;

        public static NetworkClientManagerStatic networkClientManagerStatic = null;

        public static IMessageQueue messageQueue = null;

        // Private data
        private StatsConsoleScreen? statsConsoleScreen = null;

        private GenericAPIServer? apiServer = null;

        private Thread? mainLoopThread;

        private bool running = false;

        private bool generatedNewWallet = false;


        public Node()
        {
            Logging.info("Initing node constructor");

            init();
        }

        private void init()
        {
            IxianHandler.init(Config.version, this, Config.networkType, false, Config.checksumLock);

            // Load or Generate the wallet
            if (!initWallet())
            {
                running = false;
                IxianHandler.forceShutdown = true;
                return;
            }

            initMessageQueue();

            PeerStorage.init(Config.userFolder);

            // Network configuration
            networkClientManagerStatic = new NetworkClientManagerStatic(Config.maxRelaySectorNodesToConnectTo);
            NetworkClientManager.init(networkClientManagerStatic);

            // Prepare the stream processor
            streamProcessor = new StreamProcessor(new ICPendingMessageProcessor(Config.userFolder, false), Config.streamCapabilities);

            // Init TIV
            tiv = new TransactionInclusion(new ICTransactionInclusionCallbacks(), false);

            Logging.info("Initing local storage");

            // Prepare the local storage
            IxianHandler.localStorage = new LocalStorage(Config.userFolder, new ICLocalStorageCallbacks());

            FriendList.init(Config.userFolder);

            UpdateVerify.init(Config.checkVersionUrl, Config.checkVersionSeconds);

            // TODO Maybe enable push notifications at some point

            InventoryCache.init(new InventoryCacheClient(tiv));

            RelaySectors.init(CoreConfig.relaySectorLevels, null);

            Logging.info("Node init done");
        }

        public void initMessageQueue()
        {
            switch (Config.mqDriver)
            {
                case MqDrivers.None:
                    Logging.warn("No Message Queue Driver specified, using DummyQueue Driver.");
                    messageQueue = new DummyQueue("Ixian", new JsonStreamMessageSerializer());
                    break;
                case MqDrivers.Memory:
                    messageQueue = new MemoryQueue("Ixian", new JsonStreamMessageSerializer());
                    break;
                case MqDrivers.MQTT:
                    messageQueue = new MqttQueue("Ixian", new JsonStreamMessageSerializer(), Config.mqHost, Config.mqPort);
                    break;
                case MqDrivers.RabbitMQ:
                    messageQueue = new RabbitMqQueue("Ixian", new JsonStreamMessageSerializer(), Config.mqHost, Config.mqPort);
                    break;
                default:
                    throw new Exception("Unknown Message Queue Driver.");
            }
            messageQueue?.ConnectAsync();
        }

        public void start(bool verboseConsoleOutput)
        {
            if (running)
            {
                return;
            }
            Logging.info("Starting node");

            running = true;

            // Start local storage
            IxianHandler.localStorage.start();
            IxianHandler.localStorage.nickname = Config.friendlyName;

            FriendList.loadContacts();

            UpdateVerify.start();

            ulong block_height = 0;
            byte[] block_checksum = null;

            string headers_path;
            if (IxianHandler.isTestNet)
            {
                headers_path = Path.Combine(Config.userFolder, "testnet-headers");
            }
            else
            {
                headers_path = Path.Combine(Config.userFolder, "headers");

                block_height = CoreConfig.bakedBlockHeight;
                block_checksum = CoreConfig.bakedBlockChecksum;
            }

            // TODO: replace the TIV with a liteclient-optimized implementation
            // Start TIV
            tiv.start(headers_path, block_height, block_checksum, true);

            // Generate presence list
            PresenceList.init(IxianHandler.publicIP, 0, 'C', CoreConfig.clientKeepAliveInterval);

            // Start the network queue
            NetworkQueue.start();

            streamProcessor.start();

            // Start the keepalive thread
            PresenceList.startKeepAlive();

            ActivityStorage.prepareStorage();

            mainLoopThread = new Thread(mainLoop);
            mainLoopThread.Name = "Main_Loop_Thread";
            mainLoopThread.Start();

            if (Config.apiBinds.Count == 0)
            {
                Config.apiBinds.Add("http://localhost:" + Config.apiPort + "/");
            }

            apiServer = new APIServer();
            apiServer.start(Config.apiBinds, Config.apiUsers, Config.apiAllowedIps);

            Logging.info("Node started");

            // Setup the stats console
            statsConsoleScreen = new StatsConsoleScreen();

            // Prepare stats screen
            ConsoleHelpers.verboseConsoleOutput = verboseConsoleOutput;
            Logging.consoleOutput = verboseConsoleOutput;
            Logging.flush();
            if (ConsoleHelpers.verboseConsoleOutput == false)
            {
                statsConsoleScreen.clearScreen();
            }

            connectToNetwork();
        }

        static public void connectToNetwork()
        {
            // Start the network client manager
            NetworkClientManager.start(2);

            // Start the s2 client manager
            StreamClientManager.start(Config.maxConnectedStreamingNodes);
        }

        // Handle timer routines
        public void mainLoop()
        {
            byte[] primaryAddress = IxianHandler.getWalletStorage().getPrimaryAddress().addressNoChecksum;
            if (primaryAddress == null)
                return;

            byte[] getBalanceBytes;
            using (MemoryStream mw = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(mw))
                {
                    writer.WriteIxiVarInt(primaryAddress.Length);
                    writer.Write(primaryAddress);
                }
                getBalanceBytes = mw.ToArray();
            }

            while (running)
            {
                try
                {
                    // Update the friendlist
                    updateFriendStatuses();

                    // Cleanup the presence list
                    // TODO: optimize this by using a different thread perhaps
                    PresenceList.performCleanup();

                    Balance balance = IxianHandler.balances.First();
                    // Request initial wallet balance
                    if (balance.blockHeight == 0 || balance.lastUpdate + 300 < Clock.getTimestamp())
                    {
                        CoreProtocolMessage.broadcastProtocolMessage(['M', 'H', 'R'], ProtocolMessageCode.getBalance2, getBalanceBytes, null);
                        CoreProtocolMessage.fetchSectorNodes(IxianHandler.primaryWalletAddress, CoreConfig.maxRelaySectorNodesToRequest);
                        //ProtocolMessage.fetchAllFriendsSectorNodes(10);
                        //StreamProcessor.fetchAllFriendsPresences(10);
                    }
                }
                catch (Exception e)
                {
                    Logging.error("Exception occured in mainLoop: " + e);
                }
                Thread.Sleep(2500);
            }
        }

        static public void updateFriendStatuses()
        {
            lock (FriendList.friends)
            {
                // Go through each friend and check for the pubkey in the PL
                foreach (Friend friend in FriendList.friends)
                {
                    Presence presence = null;

                    try
                    {
                        presence = PresenceList.getPresenceByAddress(friend.walletAddress);
                    }
                    catch (Exception e)
                    {
                        Logging.error("Presence Error {0}", e.Message);
                        presence = null;
                    }

                    if (presence != null)
                    {
                        if (friend.online == false)
                        {
                            friend.online = true;
                            messageQueue.PublishAsync(MQTopics.FriendStatusUpdate, friend);
                        }
                    }
                    else
                    {
                        if (friend.online == true
                            && Clock.getNetworkTimestamp() - friend.updatedStreamingNodes > CoreConfig.requestPresenceTimeout)
                        {
                            friend.online = false;
                            messageQueue.PublishAsync(MQTopics.FriendStatusUpdate, friend);
                        }
                    }
                }
            }
        }

        public void stop()
        {
            if (!running)
            {
                Logging.stop();
                IxianHandler.status = NodeStatus.stopped;
                return;
            }

            Logging.info("Stopping node...");
            running = false;

            if (messageQueue != null)
            {
                messageQueue.DisconnectAsync();
                messageQueue = null;
            }

            // Stop the stream processor
            streamProcessor.stop();

            IxianHandler.localStorage.stop();

            // Stop TIV
            tiv.stop();

            // Stop the keepalive thread
            PresenceList.stopKeepAlive();

            // Stop the API server
            if (apiServer != null)
            {
                apiServer.stop();
                apiServer = null;
            }

            ActivityStorage.stopStorage();

            // Stop the network queue
            NetworkQueue.stop();

            NetworkClientManager.stop();
            StreamClientManager.stop();

            UpdateVerify.stop();

            if (mainLoopThread != null)
            {
                mainLoopThread.Interrupt();
                mainLoopThread.Join();
                mainLoopThread = null;
            }

            IxianHandler.status = NodeStatus.stopped;

            Logging.info("Node stopped");

            Logging.stop();

            statsConsoleScreen.stop();
        }

        public override bool isAcceptingConnections()
        {
            // TODO TODO TODO TODO implement this properly
            return false;
        }


        public override void shutdown()
        {
            stop();
        }

        public override ulong getLastBlockHeight()
        {
            if (tiv.getLastBlockHeader() == null)
            {
                return 0;
            }
            return tiv.getLastBlockHeader().blockNum;
        }

        public override ulong getHighestKnownNetworkBlockHeight()
        {
            ulong bh = getLastBlockHeight();
            ulong netBlockNum = CoreProtocolMessage.determineHighestNetworkBlockNum();
            if (bh < netBlockNum)
            {
                bh = netBlockNum;
            }

            return bh;
        }

        public override int getLastBlockVersion()
        {
            if (tiv.getLastBlockHeader() == null
                || tiv.getLastBlockHeader().version < Block.maxVersion)
            {
                // TODO Omega force to v10 after upgrade
                return Block.maxVersion - 1;
            }
            return tiv.getLastBlockHeader().version;
        }

        public override bool addTransaction(Transaction tx, List<Address> relayNodeAddresses, bool force_broadcast)
        {
            // TODO Send to peer if directly connectable
            foreach (var address in relayNodeAddresses)
            {
                NetworkClientManager.sendToClient(address, ProtocolMessageCode.transactionData2, tx.getBytes(true, true), null);
            }
            //CoreProtocolMessage.broadcastProtocolMessage(new char[] { 'R' }, ProtocolMessageCode.transactionData2, tx.getBytes(true, true), null);
            PendingTransactions.addPendingLocalTransaction(tx, relayNodeAddresses);
            return true;
        }

        public override Block getLastBlock()
        {
            return tiv.getLastBlockHeader();
        }

        public override Wallet getWallet(Address id)
        {
            foreach (Balance balance in IxianHandler.balances)
            {
                if (id.addressNoChecksum.SequenceEqual(balance.address.addressNoChecksum))
                    return new Wallet(id, balance.balance);
            }
            return new Wallet(id, 0);
        }

        public override IxiNumber getWalletBalance(Address id)
        {
            foreach (Balance balance in IxianHandler.balances)
            {
                if (id.addressNoChecksum.SequenceEqual(balance.address.addressNoChecksum))
                    return balance.balance;
            }
            return 0;
        }

        // Returns the current wallet's usable balance
        public static IxiNumber getAvailableBalance()
        {
            Balance balance = IxianHandler.balances.First();
            IxiNumber currentBalance = balance.balance;
            currentBalance -= TransactionCache.getPendingSentTransactionsAmount();

            return currentBalance;
        }

        public override void parseProtocolMessage(ProtocolMessageCode code, byte[] data, RemoteEndpoint endpoint)
        {
            ProtocolMessage.parseProtocolMessage(code, data, endpoint);
        }

        public static void processPendingTransactions()
        {
            // TODO TODO improve to include failed transactions
            ulong last_block_height = IxianHandler.getLastBlockHeight();
            lock (PendingTransactions.pendingTransactions)
            {
                long cur_time = Clock.getTimestamp();
                List<PendingTransaction> tmp_pending_transactions = new List<PendingTransaction>(PendingTransactions.pendingTransactions);
                int idx = 0;
                foreach (var entry in tmp_pending_transactions)
                {
                    Transaction t = TransactionCache.getTransaction(entry.transaction.id);
                    if (t == null)
                    {
                        t = entry.transaction;
                    }
                    long tx_time = entry.addedTimestamp;

                    if (t.applied != 0)
                    {
                        PendingTransactions.pendingTransactions.RemoveAll(x => x.transaction.id.SequenceEqual(t.id));
                        continue;
                    }

                    // if transaction expired, remove it from pending transactions
                    if (last_block_height > ConsensusConfig.getRedactedWindowSize() && t.blockHeight < last_block_height - ConsensusConfig.getRedactedWindowSize())
                    {
                        Logging.error("Error sending the transaction {0}", t.getTxIdString());
                        PendingTransactions.pendingTransactions.RemoveAll(x => x.transaction.id.SequenceEqual(t.id));
                        continue;
                    }

                    if (cur_time - tx_time > 40) // if the transaction is pending for over 40 seconds, resend
                    {
                        foreach (var address in entry.relayNodeAddresses)
                        {
                            NetworkClientManager.sendToClient(address, ProtocolMessageCode.transactionData2, t.getBytes(true, true), null);
                        }
                        entry.addedTimestamp = cur_time;
                    }

                    if (entry.confirmedNodeList.Count() >= 2) // already received 2+ feedback
                    {
                        continue;
                    }

                    if (cur_time - tx_time > 30) // if the transaction is pending for over 30 seconds, send inquiry
                    {
                        CoreProtocolMessage.broadcastGetTransaction(t.id, 0);
                    }

                    idx++;
                }
            }
        }

        public override Block getBlockHeader(ulong blockNum)
        {
            return BlockHeaderStorage.getBlockHeader(blockNum);
        }

        public override IxiNumber getMinSignerPowDifficulty(ulong blockNum, int curBlockVersion, long curBlockTimestamp)
        {
            // TODO TODO implement this properly
            return ConsensusConfig.minBlockSignerPowDifficulty;
        }

        public override RegisteredNameRecord getRegName(byte[] name, bool useAbsoluteId = true)
        {
            throw new NotImplementedException();
        }
        public override byte[] getBlockHash(ulong blockNum)
        {
            Block b = getBlockHeader(blockNum);
            if (b == null)
            {
                return null;
            }

            return b.blockChecksum;
        }

        public static FriendMessage addMessageWithType(byte[] id, FriendMessageType type, Address wallet_address, int channel, string message, bool local_sender = false, Address sender_address = null, long timestamp = 0, bool fire_local_notification = true, int payable_data_len = 0)
        {
            FriendMessage friend_message = FriendList.addMessageWithType(id, type, wallet_address, channel, message, local_sender, sender_address, timestamp, fire_local_notification, payable_data_len);
            if (friend_message != null)
            {
                bool oldMessage = false;

                Friend friend = FriendList.getFriend(wallet_address);

                if (!friend.online)
                {
                    StreamProcessor.fetchFriendsPresence(friend);
                }

                // Check if the message was sent before the friend was added to the contact list
                if (friend.addedTimestamp > friend_message.timestamp)
                {
                    oldMessage = true;
                }

                if (!friend_message.read)
                {
                    // Increase the unread counter if this is a new message
                    if (!oldMessage)
                        friend.metaData.unreadMessageCount++;

                    friend.saveMetaData();
                }

                // Only send alerts if this is a new message
                if (oldMessage == false)
                {
                    messageQueue.PublishAsync(MQTopics.Chat, friend_message);
                }
            }
            return friend_message;
        }

        static public Transaction sendTransaction(Address address, IxiNumber amount)
        {
            // TODO add support for sending funds from multiple addreses automatically based on remaining balance
            Balance address_balance = IxianHandler.balances.First();
            var from = address_balance.address;
            return sendTransactionFrom(from, address, amount);
        }

        static public (Transaction transaction, List<Address> relayNodeAddresses) prepareTransactionFrom(Address fromAddress, Address toAddress, IxiNumber amount)
        {
            IxiNumber fee = ConsensusConfig.forceTransactionPrice;
            SortedDictionary<Address, ToEntry> to_list = new(new AddressComparer());
            Balance address_balance = IxianHandler.balances.FirstOrDefault(addr => addr.address.addressNoChecksum.SequenceEqual(fromAddress.addressNoChecksum));
            Address pubKey = new(IxianHandler.getWalletStorage().getPrimaryPublicKey());

            if (!IxianHandler.getWalletStorage().isMyAddress(fromAddress))
            {
                Logging.info("From address is not my address.");
                return (null, null);
            }

            SortedDictionary<byte[], IxiNumber> from_list = new(new ByteArrayComparer())
            {
                { IxianHandler.getWalletStorage().getAddress(fromAddress).nonce, amount }
            };

            to_list.AddOrReplace(toAddress, new ToEntry(Transaction.getExpectedVersion(IxianHandler.getLastBlockVersion()), amount));

            List<Address> relayNodeAddresses = NetworkClientManager.getRandomConnectedClientAddresses(2);
            IxiNumber relayFee = 0;
            foreach (Address relayNodeAddress in relayNodeAddresses)
            {
                var tmpFee = fee > ConsensusConfig.transactionDustLimit ? fee : ConsensusConfig.transactionDustLimit;
                to_list.AddOrReplace(relayNodeAddress, new ToEntry(Transaction.getExpectedVersion(IxianHandler.getLastBlockVersion()), tmpFee));
                relayFee += tmpFee;
            }


            // Prepare transaction to calculate fee
            Transaction transaction = new((int)Transaction.Type.Normal, fee, to_list, from_list, pubKey, IxianHandler.getHighestKnownNetworkBlockHeight());

            relayFee = 0;
            foreach (Address relayNodeAddress in relayNodeAddresses)
            {
                var tmpFee = transaction.fee > ConsensusConfig.transactionDustLimit ? transaction.fee : ConsensusConfig.transactionDustLimit;
                to_list[relayNodeAddress].amount = tmpFee;
                relayFee += tmpFee;
            }

            byte[] first_address = from_list.Keys.First();
            from_list[first_address] = from_list[first_address] + relayFee + transaction.fee;
            IxiNumber wal_bal = IxianHandler.getWalletBalance(new Address(transaction.pubKey.addressNoChecksum, first_address));
            if (from_list[first_address] > wal_bal)
            {
                IxiNumber maxAmount = wal_bal - transaction.fee;

                if (maxAmount < 0)
                    maxAmount = 0;

                Logging.info("Insufficient funds to cover amount and transaction fee.\nMaximum amount you can send is {0} IXI.\n", maxAmount);
                return (null, null);
            }
            // Prepare transaction with updated "from" amount to cover fee
            transaction = new((int)Transaction.Type.Normal, fee, to_list, from_list, pubKey, IxianHandler.getHighestKnownNetworkBlockHeight());
            return (transaction, relayNodeAddresses);
        }

        static public Transaction sendTransactionFrom(Address fromAddress, Address toAddress, IxiNumber amount)
        {
            var prepTx = prepareTransactionFrom(fromAddress, toAddress, amount);
            var transaction = prepTx.transaction;
            var relayNodeAddresses = prepTx.relayNodeAddresses;
            // Send the transaction
            if (IxianHandler.addTransaction(transaction, relayNodeAddresses, true))
            {
                Logging.info("Sending transaction, txid: {0}", transaction.getTxIdString());
                return transaction;
            }
            else
            {
                Logging.warn("Could not send transaction, txid: {0}", transaction.getTxIdString());
            }
            return null;
        }

        // Cleans the storage cache and logs
        public static bool cleanCacheAndLogs()
        {
            ActivityStorage.deleteCache();

            PeerStorage.deletePeersFile();

            Logging.clear();

            Logging.info("Cleaned cache and logs.");
            return true;
        }

        private bool initWallet()
        {
            WalletStorage walletStorage = new WalletStorage(Config.walletFile);

            Logging.flush();

            if (!walletStorage.walletExists())
            {
                ConsoleHelpers.displayBackupText();

                // Request a password
                // NOTE: This can only be done in testnet to enable automatic testing!
                string password = "";
                if (Config.dangerCommandlinePasswordCleartextUnsafe != "")
                {
                    Logging.warn("TestNet detected and wallet password has been specified on the command line!");
                    password = Config.dangerCommandlinePasswordCleartextUnsafe;
                    // Also note that the commandline password still has to be >= 10 characters
                }
                while (password.Length < 10)
                {
                    Logging.flush();
                    password = ConsoleHelpers.requestNewPassword("Enter a password for your new wallet: ");
                    if (IxianHandler.forceShutdown)
                    {
                        return false;
                    }
                }
                walletStorage.generateWallet(password);
                generatedNewWallet = true;
            }
            else
            {
                ConsoleHelpers.displayBackupText();

                bool success = false;
                while (!success)
                {

                    // NOTE: This is only permitted on the testnet for dev/testing purposes!
                    string password = "";
                    if (Config.dangerCommandlinePasswordCleartextUnsafe != "")
                    {
                        Logging.warn("Attempting to unlock the wallet with a password from commandline!");
                        password = Config.dangerCommandlinePasswordCleartextUnsafe;
                    }
                    if (password.Length < 10)
                    {
                        Logging.flush();
                        Console.Write("Enter wallet password: ");
                        password = ConsoleHelpers.getPasswordInput();
                    }
                    if (IxianHandler.forceShutdown)
                    {
                        return false;
                    }
                    if (walletStorage.readWallet(password))
                    {
                        success = true;
                    }
                }
            }


            if (walletStorage.getPrimaryPublicKey() == null)
            {
                return false;
            }

            // Wait for any pending log messages to be written
            Logging.flush();

            Console.WriteLine();
            Console.WriteLine("Your IXIAN addresses are: ");
            Console.ForegroundColor = ConsoleColor.Green;
            foreach (var entry in walletStorage.getMyAddressesBase58())
            {
                Console.WriteLine(entry);
            }
            Console.ResetColor();
            Console.WriteLine();

            if (Config.onlyShowAddresses)
            {
                return false;
            }

            // Check if we should change the password of the wallet
            if (Config.changePass == true)
            {
                // Request a new password
                string new_password = "";
                while (new_password.Length < 10)
                {
                    new_password = ConsoleHelpers.requestNewPassword("Enter a new password for your wallet: ");
                    if (IxianHandler.forceShutdown)
                    {
                        return false;
                    }
                }
                walletStorage.writeWallet(new_password);
                return false;
            }

            Logging.info("Public Node Address: {0}", walletStorage.getPrimaryAddress().ToString());


            if (walletStorage.viewingWallet)
            {
                Logging.error("Viewing-only wallet {0} cannot be used as the primary DLT Node wallet.", walletStorage.getPrimaryAddress().ToString());
                return false;
            }

            IxianHandler.addWallet(walletStorage);

            // Prepare the balances list
            List<Address> address_list = IxianHandler.getWalletStorage().getMyAddresses();
            foreach (Address addr in address_list)
            {
                IxianHandler.balances.Add(new Balance(addr, 0));
            }

            return true;
        }
    }
}