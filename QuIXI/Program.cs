using QuIXI.Meta;
using IXICore;
using IXICore.Meta;
using IXICore.Utils;
using System.Reflection;

namespace QuIXI
{
    class Program
    {
        private static Node? node = null;

        static void Main(string[] args)
        {
            if (!Console.IsOutputRedirected)
            {
                // There are probably more problematic Console operations if we're working in stdout redirected mode, but 
                // this one is blocking automated testing.
                Console.Clear();
            }

            // Start logging
            if (!Logging.start(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), Config.logVerbosity))
            {
                Logging.info("Press ENTER to exit.");
                Console.ReadLine();
                return;
            }

            Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e) {
                ConsoleHelpers.verboseConsoleOutput = true;
                Logging.consoleOutput = ConsoleHelpers.verboseConsoleOutput;
                e.Cancel = true;
                IxianHandler.forceShutdown = true;
            };

            if (onStart(args))
            {
                mainLoop();
            }

            onStop();
        }

        static bool onStart(string[] args)
        {
            // Read configuration from command line
            if (!Config.init(args))
            {
                Environment.Exit(2);
                return false;
            }

            // Set the logging options
            Logging.setOptions(Config.maxLogSize, Config.maxLogCount);
            Logging.flush();

            Logging.info("Starting QuIXI {0} ({1})", Config.version, CoreConfig.version);
            Logging.info("Operating System is {0}", Platform.getOSNameAndVersion());

            // Log the parameters to notice any changes
            Logging.info("API Port: {0}", Config.apiPort);
            Logging.info("Wallet File: {0}", Config.walletFile);

            // Initialize the node
            node = new Node();

            if (IxianHandler.forceShutdown)
            {
                Thread.Sleep(1000);
                return false;
            }

            // Start the node
            node.start(Config.verboseOutput);

            if (ConsoleHelpers.verboseConsoleOutput)
                Console.WriteLine("-----------\nPress Ctrl-C or use the /shutdown API to stop the QuIXI process at any time.\n");

            return true;
        }

        static void mainLoop()
        {
            while (!IxianHandler.forceShutdown)
            {
                try
                {
                    if (!Console.IsInputRedirected && Console.KeyAvailable)
                    {
                        ConsoleKeyInfo key = Console.ReadKey();

                        if (key.Key == ConsoleKey.V)
                        {
                            ConsoleHelpers.verboseConsoleOutput = !ConsoleHelpers.verboseConsoleOutput;
                            Logging.consoleOutput = ConsoleHelpers.verboseConsoleOutput;
                            Console.CursorVisible = ConsoleHelpers.verboseConsoleOutput;
                            Console.Clear();
                        }
                        else if (key.Key == ConsoleKey.Escape)
                        {
                            ConsoleHelpers.verboseConsoleOutput = true;
                            Logging.consoleOutput = ConsoleHelpers.verboseConsoleOutput;
                            IxianHandler.forceShutdown = true;
                        }

                    }

                }
                catch (Exception e)
                {
                    Logging.error("Exception occured in mainLoop: " + e);
                }
                Thread.Sleep(1000);
            }
        }

        static void onStop()
        {
            // Stop the node
            node?.stop();

            // Stop logging
            Logging.flush();
            Logging.stop();

            Console.WriteLine("");
            Console.WriteLine("QuIXI stopped.");
        }
    }
}
