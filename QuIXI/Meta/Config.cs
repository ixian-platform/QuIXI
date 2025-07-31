using Fclp;
using QuIXI.MQ.Drivers;
using IXICore;
using IXICore.Meta;
using IXICore.Network;
using IXICore.Streaming;
using System.Text;

namespace QuIXI.Meta
{
    public class Config
    {
        // Read-only values
        public static readonly string version = "xcc-0.9.0";

        public static readonly string checkVersionUrl = "https://resources.ixian.io/xcc-update.txt";
        public static readonly int checkVersionSeconds = 6 * 60 * 60; // 6 hours

        public static readonly int maximumStreamClients = 1000; // Maximum number of stream clients this server can accept

        public static int maxRelaySectorNodesToConnectTo = 3;
        public static int maxConnectedStreamingNodes = 6;

        // Options
        public static NetworkType networkType = NetworkType.main;
        public static byte[] checksumLock = null;

        public static int apiPort = 8001;
        public static int serverPort = 0;

        public static string userFolder = "";

        public static Dictionary<string, string> apiUsers = new Dictionary<string, string>();

        public static List<string> apiAllowedIps = new List<string>();
        public static List<string> apiBinds = new List<string>();

        public static string configFilename = "ixian.cfg";
        public static string walletFile = "ixian.wal";

        public static int maxLogSize = 50;
        public static int maxLogCount = 10;

        public static int logVerbosity = (int)LogSeverity.info + (int)LogSeverity.warn + (int)LogSeverity.error;
        public static bool verboseOutput = false;

        public static bool onlyShowAddresses = false;

        public static string friendlyName = "QuIXI";

        // Store the device id in a cache for reuse in later instances
        public static string externalIp = "";

        // Development/Testing options
        public static string dangerCommandlinePasswordCleartextUnsafe = "";


        // internal
        public static bool changePass = false;


        // Message Queue Service settings
        public static MqDrivers mqDriver = MqDrivers.None;
        public static string mqHost = null;
        public static int mqPort = 0;

        // expose own external IP:Port to PL
        public static bool directConnection = false;

        public static StreamCapabilities streamCapabilities = StreamCapabilities.Incoming | StreamCapabilities.AppProtocols;

        private Config()
        {

        }

        private static void outputHelp()
        {
            Console.WriteLine("Starts a new instance of QuIXI");
            Console.WriteLine("");
            Console.WriteLine(" QuIXI.exe [-h] [-v] [-t] [-x] [-c] [-a 8001] [-i ip] [-w ixian.wal] [-n seed1.ixian.io:10234]");
            Console.WriteLine(" [--config ixian.cfg] [--maxLogSize 50] [--maxLogCount 10]  [--logVerbosity 14]");
            Console.WriteLine(" [--walletPassword] [--checksumLock Ixian] [--verboseOutput]");
            Console.WriteLine("");
            Console.WriteLine("    -h\t\t\t Displays this help");
            Console.WriteLine("    -v\t\t\t Displays version");
            Console.WriteLine("    -t\t\t\t Starts node in testnet mode");
            Console.WriteLine("    -x\t\t\t Change password of an existing wallet");
            Console.WriteLine("    -c\t\t\t Removes cache, peers.dat and ixian.log files before starting");
            Console.WriteLine("    -a\t\t\t HTTP/API port to listen on");
            Console.WriteLine("    -i\t\t\t External IP Address to use");
            Console.WriteLine("    -w\t\t\t Specify location of the ixian.wal file");
            Console.WriteLine("    -n\t\t\t Specify which seed node to use");
            Console.WriteLine("    --config\t\t Specify config filename (default ixian.cfg)");
            Console.WriteLine("    --maxLogSize\t Specify maximum log file size in MB");
            Console.WriteLine("    --maxLogCount\t Specify maximum number of log files");
            Console.WriteLine("    --logVerbosity\t Sets log verbosity (0 = none, trace = 1, info = 2, warn = 4, error = 8)");
            Console.WriteLine("    --checksumLock\t\t Sets the checksum lock for seeding checksums - useful for custom networks.");
            Console.WriteLine("    --verboseOutput\t\t Starts node with verbose output.");
            Console.WriteLine("    --networkType\t\t mainnet, testnet or regtest.");
            Console.WriteLine("");
            Console.WriteLine("----------- Developer CLI flags -----------");
            Console.WriteLine("    --walletPassword\t Specify the password for the wallet.");
            Console.WriteLine("");
            Console.WriteLine("----------- Config File Options -----------");
            Console.WriteLine(" Config file options should use parameterName = parameterValue semantics.");
            Console.WriteLine(" Each option should be specified in its own line. Example:");
            Console.WriteLine("    apiPort = 8001");
            Console.WriteLine("");
            Console.WriteLine(" Available options:");
            Console.WriteLine("    apiPort\t\t HTTP/API port to listen on (same as -a CLI)");
            Console.WriteLine("    apiAllowIp\t\t Allow API connections from specified source or sources (can be used multiple times)");
            Console.WriteLine("    apiBind\t\t Bind to given address to listen for API connections (can be used multiple times)");
            Console.WriteLine("    testnetApiPort\t HTTP/API port to listen on in testnet mode (same as -a CLI)");
            Console.WriteLine("    addApiUser\t\t Adds user:password that can access the API (can be used multiple times)");

            Console.WriteLine("    externalIp\t\t External IP Address to use (same as -i CLI)");
            Console.WriteLine("    addPeer\t\t Specify which seed node to use (same as -n CLI) (can be used multiple times)");
            Console.WriteLine("    addTestnetPeer\t Specify which seed node to use in testnet mode (same as -n CLI) (can be used multiple times)");
            Console.WriteLine("    maxLogSize\t\t Specify maximum log file size in MB (same as --maxLogSize CLI)");
            Console.WriteLine("    maxLogCount\t\t Specify maximum number of log files (same as --maxLogCount CLI)");
            Console.WriteLine("    logVerbosity\t Sets log verbosity (same as --logVerbosity CLI)");
            Console.WriteLine("    walletNotify\t Execute command when a wallet transaction changes");
            Console.WriteLine("    mqDriver\t\t Message Queue Driver - mqtt or rabbitmq");
            Console.WriteLine("    mqHost\t\t Message Queue Hostname");
            Console.WriteLine("    mqPort\t\t Message Queue port");
            Console.WriteLine("    streamCapabilities\t Stream capabilities - Incoming, Outgoing, IPN, Apps, AppProtocols");

            Environment.Exit(0);
        }

        private static void outputVersion()
        {
            // Do nothing but exit since version is the first thing displayed

            Environment.Exit(0);
        }

        private static NetworkType parseNetworkTypeValue(string value)
        {
            NetworkType netType;
            value = value.ToLower();
            switch (value)
            {
                case "mainnet":
                    netType = NetworkType.main;
                    break;
                case "testnet":
                    netType = NetworkType.test;
                    break;
                case "regtest":
                    netType = NetworkType.reg;
                    break;
                default:
                    throw new Exception(string.Format("Unknown network type '{0}'. Possible values are 'mainnet', 'testnet', 'regtest'", value));
            }
            return netType;
        }

        private static MqDrivers parseMqDriverValue(string value)
        {
            MqDrivers mqDriver;
            value = value.ToLower();
            switch (value)
            {
                case "none":
                    mqDriver = MqDrivers.None;
                    break;
                case "mqtt":
                    mqDriver = MqDrivers.MQTT;
                    break;
                case "rabbitmq":
                    mqDriver = MqDrivers.RabbitMQ;
                    break;
                default:
                    throw new Exception(string.Format("Unknown Message Queue Driver '{0}'. Possible values are 'none', 'mqtt', 'rabbitmq'", value));
            }
            return mqDriver;
        }

        private static void readConfigFile(string filename)
        {
            if (!File.Exists(filename))
            {
                return;
            }
            Logging.info("Reading config file: " + filename);
            bool foundAddPeer = false;
            bool foundAddTestPeer = false;
            List<string> lines = File.ReadAllLines(filename).ToList();
            foreach (string line in lines)
            {
                string[] option = line.Split('=');
                if (option.Length < 2)
                {
                    continue;
                }
                string key = option[0].Trim([' ', '\t', '\r', '\n']);
                string value = option[1].Trim([' ', '\t', '\r', '\n']);

                if (key.StartsWith(";"))
                {
                    continue;
                }
                Logging.info("Processing config parameter '" + key + "' = '" + value + "'");
                switch (key)
                {
                    case "serverPort":
                        serverPort = int.Parse(value);
                        break;
                    case "apiPort":
                        apiPort = int.Parse(value);
                        break;
                    case "apiAllowIp":
                        apiAllowedIps.Add(value);
                        break;
                    case "apiBind":
                        apiBinds.Add(value);
                        break;
                    case "addApiUser":
                        string[] credential = value.Split(':');
                        if (credential.Length == 2)
                        {
                            apiUsers.Add(credential[0], credential[1]);
                        }
                        break;
                    case "externalIp":
                        externalIp = value;
                        break;
                    case "addPeer":
                        if (!foundAddPeer)
                        {
                            NetworkUtils.seedNodes.Clear();
                        }
                        foundAddPeer = true;
                        NetworkUtils.seedNodes.Add([value, null]);
                        break;
                    case "addTestnetPeer":
                        if (!foundAddTestPeer)
                        {
                            NetworkUtils.seedTestNetNodes.Clear();
                        }
                        foundAddTestPeer = true;
                        NetworkUtils.seedTestNetNodes.Add([value, null]);
                        break;
                    case "maxLogSize":
                        maxLogSize = int.Parse(value);
                        break;
                    case "maxLogCount":
                        maxLogCount = int.Parse(value);
                        break;
                    case "walletNotify":
                        CoreConfig.walletNotifyCommand = value;
                        break;
                    case "logVerbosity":
                        logVerbosity = int.Parse(value);
                        break;
                    case "checksumLock":
                        checksumLock = Encoding.UTF8.GetBytes(value);
                        break;
                    case "networkType":
                        networkType = parseNetworkTypeValue(value);
                        break;
                    case "mqDriver":
                        mqDriver = parseMqDriverValue(value);
                        break;
                    case "mqHost":
                        mqHost = value;
                        break;
                    case "mqPort":
                        mqPort = int.Parse(value);
                        break;
                    case "streamCapabilites":
                        streamCapabilities = (StreamCapabilities)int.Parse(value);
                        break;
                    default:
                        // unknown key
                        Logging.warn("Unknown config parameter was specified '" + key + "'");
                        break;
                }
            }
        }
        public static bool init(string[] args)
        {
            // first pass
            var cmd_parser = new FluentCommandLineParser();

            // help
            cmd_parser.SetupHelp("h", "help").Callback(text => outputHelp());

            // config file
            cmd_parser.Setup<string>("config").Callback(value => configFilename = value).Required();

            cmd_parser.Parse(args);

            readConfigFile(configFilename);



            // second pass
            cmd_parser = new FluentCommandLineParser();

            // testnet
            cmd_parser.Setup<bool>('t', "testnet").Callback(value => networkType = NetworkType.test).Required();
            cmd_parser.Setup<string>("networkType").Callback(value => networkType = parseNetworkTypeValue(value)).Required();

            cmd_parser.Parse(args);


            string seedNode = "";

            // third pass
            cmd_parser = new FluentCommandLineParser();

            bool start_clean = false; // Flag to determine if node should delete cache+logs

            // version
            cmd_parser.Setup<bool>('v', "version").Callback(text => outputVersion());

            // Check for password change
            cmd_parser.Setup<bool>('x', "changepass").Callback(value => changePass = value).Required();

            // Check for clean parameter
            cmd_parser.Setup<bool>('c', "clean").Callback(value => start_clean = value).Required();

            cmd_parser.Setup<int>('a', "apiport").Callback(value => apiPort = value).Required();

            cmd_parser.Setup<string>('i', "ip").Callback(value => externalIp = value).Required();

            cmd_parser.Setup<string>('w', "wallet").Callback(value => walletFile = value).Required();

            cmd_parser.Setup<string>('n', "node").Callback(value => seedNode = value).Required();

            cmd_parser.Setup<int>("maxLogSize").Callback(value => maxLogSize = value).Required();

            cmd_parser.Setup<int>("maxLogCount").Callback(value => maxLogCount = value).Required();

            cmd_parser.Setup<bool>("onlyShowAddresses").Callback(value => onlyShowAddresses = true).Required();

            // Debug

            cmd_parser.Setup<string>("walletPassword").Callback(value => dangerCommandlinePasswordCleartextUnsafe = value).SetDefault("");

            cmd_parser.Setup<int>("logVerbosity").Callback(value => logVerbosity = value).Required();

            cmd_parser.Setup<bool>("verboseOutput").Callback(value => verboseOutput = value).SetDefault(false);

            cmd_parser.Parse(args);


            // Validate parameters

            if (start_clean)
            {
                Node.cleanCacheAndLogs();
            }

            if (seedNode != "")
            {
                switch (networkType)
                {
                    case NetworkType.main:
                        NetworkUtils.seedNodes = new List<string[]>
                            {
                                new string[2] { seedNode, null }
                            };
                        break;

                    case NetworkType.test:
                    case NetworkType.reg:
                        NetworkUtils.seedTestNetNodes = new List<string[]>
                            {
                                new string[2] { seedNode, null }
                            };
                        break;
                }
            }

            return true;
        }
    }
}