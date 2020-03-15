using IOT.Client;
using IOT.Core.Helper;
using IOT.Core.Messaging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOT.Device
{
    internal static class Program
    {
        private const string THING_ID = "thingId";
        private const string THING_COMMAND_TOPIC = "thingCommandTopic";
        private const string THING_CONNECTION_STRING = "thingConnectionString";

        private static void Main(string[] args)
        {
            string ns, 
                   scheme;
            var runClient = true;
            var thingId = new Guid(ConfigurationManager.AppSettings[THING_ID]);
            var thingCommandTopic = int.Parse(ConfigurationManager.AppSettings[THING_COMMAND_TOPIC]);
            var thingConnectionString = ConfigurationManager.AppSettings[THING_CONNECTION_STRING];

            ProcessCommandLineParameters(args, ref thingId, ref thingCommandTopic, ref thingConnectionString);
            ServiceBusConnection.ToIssuer(thingConnectionString, out scheme, out ns);

            do
            {
                FormatConsole(ns, thingCommandTopic);
                Console.Clear();

                Console.WriteLine("Internet Of Things - Device '{0}'", thingId);
                Console.WriteLine();

                Console.WriteLine("a. Send Message To Ingestion Topic");
                Console.WriteLine("b. Receive Message From Command Topic");
                Console.WriteLine("x. Enter Thing Connection Parameter");

                Console.WriteLine();
                Console.WriteLine("0. Exit Application");
                Console.WriteLine();

                try
                {
                    switch (Console.ReadKey(true).KeyChar)
                    {
                        case 'a':
                            SendMessageToIngestionTopic(thingId, thingCommandTopic, thingConnectionString);
                            break;

                        case 'b':
                            ReceiveMessageFromCommandTopic(thingId, thingCommandTopic, thingConnectionString);
                            break;

                        case 'x':
                            EnterThingConnectionParameters(out thingId, out thingCommandTopic, out thingConnectionString);
                            break;

                        case '0':
                            runClient = false;
                            break;

                        default:
                            Console.WriteLine("Key ignored!");
                            break;
                    }

                    if (runClient)
                        Console.WriteLine("Operation completed!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\n\nCaught exception: {0}.\n\n", ex);
                }

                if (runClient)
                    Console.ReadKey(true);
            } while (runClient);
        }

        #region Common Helpers
        private static void FormatConsole(string ns, int thingCommandTopic,
                                          int width = 90, int height = 25, 
                                          int bufferWidth = 90, int bufferHeight = 256)
        {
            Console.Title = string.Format("{0}: {1:00}", ns, thingCommandTopic);

            Console.WindowWidth = width;
            Console.WindowHeight = height;
            Console.BufferWidth = bufferWidth;
            Console.BufferHeight = bufferHeight;

            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkBlue;
        }

        private static void SendMessageToIngestionTopic(Guid thingId, int thingCommandTopic, string thingConnectionString)
        {
            bool broadcast;
            Guid? to;

            using (var ta = new ThingsAccess(thingId, thingCommandTopic, thingConnectionString))
            {
                Console.Write("Content Type: ");

                switch (Console.ReadLine().ToLower())
                {
                    case "event":
                        ta.SendEvent(new EventMessage { Type = "Test", Body = Encoding.Unicode.GetBytes("Hello, Event!") });
                        break;

                    case "control":
                        Console.Write("Broadcast (y/n): ");
                        broadcast = Console.ReadLine().ToLower() == "y";

                        if (!broadcast)
                        {
                            Console.Write("To: ");
                            to = new Guid(Console.ReadLine());
                        }
                        else
                            to = null;

                        ta.SendControl(new ControlMessage { Command = "Off", Parameters = null }, to, broadcast);
                        break;

                    case "analytics":
                        ta.SendAnalytics(new AnalyticsMessage { Type = "Test", Body = Encoding.Unicode.GetBytes("Hello, Analytics!") });
                        break;

                    default:
                        Console.WriteLine("Content Type must be: Event, Control, Analytics");
                        break;
                }
            }
        }

        private static void ReceiveMessageFromCommandTopic(Guid thingId, int thingCommandTopic, string thingConnectionString)
        {
            CommandMessage commandMessage;

            using (var ta = new ThingsAccess(thingId, thingCommandTopic, thingConnectionString))
            {
                if ((commandMessage = ta.ReceiveCommand(TimeSpan.FromSeconds(5))) != null)
                    Console.WriteLine("Command: {0}", commandMessage.Command);
            }
        }

        private static void EnterThingConnectionParameters(out Guid thingId, out int thingCommandTopic, out string thingConnectionString)
        {
            Console.Write("Thing Id: ");
            thingId = new Guid(Console.ReadLine());

            Console.Write("Thing Command Topic: ");
            thingCommandTopic = int.Parse(Console.ReadLine());

            Console.Write("Thing Connection String: ");
            thingConnectionString = Console.ReadLine();
        }

        private static void ProcessCommandLineParameters(string[] args,
                                                         ref Guid thingId, ref int thingCommandTopic, ref string thingConnectionString)
        {
            if (args.Length > 0)
                thingId = new Guid(args[0]);

            if (args.Length > 1)
                thingCommandTopic = int.Parse(args[1]);

            if (args.Length > 2)
                thingConnectionString = args[2];
        }
        #endregion
    }
}
