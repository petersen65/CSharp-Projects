using IOT.Server.Provisioning;
using IOT.Server.Worker;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOT.Backend
{
    internal static class Program
    {
        private const char SEPARATOR_VALUES = ' ';
        private const string THING_PARAMETERS = "{0} {1} \"{2}\"";
        private const string THING_PATH_PARAMETERS = "{0} {1} {2} \"{3}\"";
        private const string THING_SETTING_THING_ID = "REM <add key=\"thingId\" value=\"{0}\" />";
        private const string THING_SETTING_COMMAND_TOPIC = "REM <add key=\"thingCommandTopic\" value=\"{0}\" />";
        private const string THING_SETTING_CONNECTION_STRING = "REM <add key=\"thingConnectionString\" value=\"{0}\" />";
        private const string THING_COMMAND_FILENAME = "{0}.cmd";

        private const string THING_PATH = "thingPath";
        private const string PARTITION_AREA = "partitionArea";
        private const string COMMAND_TOPICS = "commandTopics";
        private const string COMMAND_SUBSCRIPTIONS = "commandSubscriptions";
        private const string SERVICE_NAMESPACE = "serviceNamespace";
        private const string SERVICE_OWNER = "serviceOwner";
        private const string SERVICE_OWNER_SECRET = "serviceOwnerSecret";
        private const string EVENT_STORE = "eventStore";
        private const string STORAGE_WRITER_CONNECTION = "storageWriterConnection";
        private const string ACS_MANAGEMENT_SERVICE_NAME = "acsManagementServiceName";
        private const string ACS_MANAGEMENT_SERVICE_SECRET = "acsManagementServiceSecret";

        private static void Main(string[] args)
        {
            var runClient = true;
            var activePartition = 0;
            var thingProcesses = new List<Process>();
            ThingsWorker thingsWorker = null;

            var thingPath = ConfigurationManager.AppSettings[THING_PATH];
            var partitionArea = ConfigurationManager.AppSettings[PARTITION_AREA].Split(SEPARATOR_VALUES);
            var commandTopics = int.Parse(ConfigurationManager.AppSettings[COMMAND_TOPICS]);
            var commandSubscriptions = int.Parse(ConfigurationManager.AppSettings[COMMAND_SUBSCRIPTIONS]);
            var serviceNamespace = ConfigurationManager.AppSettings[SERVICE_NAMESPACE].Split(SEPARATOR_VALUES);
            var serviceOwner = ConfigurationManager.AppSettings[SERVICE_OWNER];
            var serviceOwnerSecret = ConfigurationManager.AppSettings[SERVICE_OWNER_SECRET].Split(SEPARATOR_VALUES);
            var eventStore = ConfigurationManager.AppSettings[EVENT_STORE];
            var storageWriterConnection = ConfigurationManager.AppSettings[STORAGE_WRITER_CONNECTION];
            var acsManagementServiceName = ConfigurationManager.AppSettings[ACS_MANAGEMENT_SERVICE_NAME];
            var acsManagementServiceSecret = ConfigurationManager.AppSettings[ACS_MANAGEMENT_SERVICE_SECRET].Split(SEPARATOR_VALUES);

            ProcessCommandLineParameters(args, partitionArea.Length, ref activePartition);

            do
            {
                FormatConsole(serviceNamespace[activePartition], commandTopics, commandSubscriptions);
                Console.Clear();

                Console.WriteLine("Internet Of Things - Area '{0}'", partitionArea[activePartition]);
                Console.WriteLine();

                Console.WriteLine("a. Allocate Partition");
                Console.WriteLine("b. Register Thing");
                Console.WriteLine("c. Register Ingestion Topic Receiver");
                Console.WriteLine("x. Choose Active Partition");

                Console.WriteLine();
                Console.WriteLine("0. Exit Application");
                Console.WriteLine();

                try
                {
                    switch (Console.ReadKey(true).KeyChar)
                    {
                        case 'a':
                            AllocatePartition(commandTopics, commandSubscriptions,
                                              partitionArea[activePartition], serviceNamespace[activePartition], eventStore,
                                              serviceOwner, serviceOwnerSecret[activePartition], storageWriterConnection,
                                              acsManagementServiceName, acsManagementServiceSecret[activePartition]);

                            break;

                        case 'b':
                            thingProcesses.Add(RegisterThing(thingPath,
                                                             commandTopics, commandSubscriptions, partitionArea[activePartition]));
                            break;

                        case 'c':
                            thingsWorker = RegisterIngestionTopicReceiver(thingsWorker, 
                                                                          serviceNamespace[activePartition], 
                                                                          serviceOwner, serviceOwnerSecret[activePartition], 
                                                                          storageWriterConnection);
                            break;

                        case 'x':
                            ChooseActivePartition(partitionArea.Length, ref activePartition, ref thingsWorker);
                            break;

                        case '0':
                            StopThingProcesses(thingProcesses);

                            if (thingsWorker != null)
                                thingsWorker.Dispose();

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
        private static void FormatConsole(string ns, int commandTopics, int commandSubscriptions,
                                          int width = 90, int height = 25,
                                          int bufferWidth = 90, int bufferHeight = 256)
        {
            Console.Title = string.Format("{0}: {1},{2}", ns, commandTopics, commandSubscriptions);

            Console.WindowWidth = width;
            Console.WindowHeight = height;
            Console.BufferWidth = bufferWidth;
            Console.BufferHeight = bufferHeight;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.BackgroundColor = ConsoleColor.DarkCyan;
        }

        private static void AllocatePartition(int commandTopics, int commandSubscriptions, 
                                              string partitionArea, string serviceNamespace, string eventStore,
                                              string serviceOwner, string serviceOwnerSecret, string storageWriterConnection,
                                              string acsManagementServiceName, string acsManagementServiceSecret)
        {
            PartitionDescription pd;

            using (var iot = new InternetOfThings(commandTopics, commandSubscriptions))
            {
                pd = new PartitionDescription(DateTime.UtcNow.ToLongDateString(),
                                              partitionArea,
                                              serviceNamespace,
                                              eventStore)
                {
                    Owner = serviceOwner,
                    OwnerSecret = serviceOwnerSecret,
                    StorageAccount = storageWriterConnection,
                    AccessControl = acsManagementServiceName,
                    AccessControlSecret = acsManagementServiceSecret
                };

                pd = iot.CreatePartition(pd);
                iot.ActivatePartition(pd);
            }
        }

        private static Process RegisterThing(string thingPath, int commandTopics, int commandSubscriptions, string partitionArea)
        {
            ThingDescription td;

            using (var iot = new InternetOfThings(commandTopics, commandSubscriptions))
            {
                td = new ThingDescription(DateTime.UtcNow.ToLongDateString(), partitionArea);

                td = iot.CreateThing(td);
                iot.ActivateThing(td);
            }

            return StartThingProcess(thingPath, td.Id, td.CommandTopic, td.ConnectionString);
        }

        private static ThingsWorker RegisterIngestionTopicReceiver(ThingsWorker thingsWorker,
                                                                   string serviceNamespace, 
                                                                   string serviceOwner, string serviceOwnerSecret, 
                                                                   string storageWriterConnection)
        {
            if (thingsWorker != null)
                thingsWorker.UnregisterReceiver();
            else
            {
                thingsWorker = new ThingsWorker(storageWriterConnection,
                                                serviceNamespace,
                                                serviceOwner,
                                                serviceOwnerSecret);
            }

            thingsWorker.RegisterReceiver();
            return thingsWorker;
        }

        private static void ChooseActivePartition(int partitionAreaLength, ref int activePartition, ref ThingsWorker thingsWorker)
        {
            int tempPartition;
            
            Console.Write("Active Partition: ");
            tempPartition = int.Parse(Console.ReadLine());

            if (tempPartition < 0 || tempPartition >= partitionAreaLength)
                Console.WriteLine("Value ignored!");
            else
            {
                if (thingsWorker != null)
                {
                    thingsWorker.UnregisterReceiver();
                    thingsWorker.Dispose();
                    thingsWorker = null;
                }

                activePartition = tempPartition;
            }
        }

        private static Process StartThingProcess(string thingPath, Guid thingId, int thingCommandTopic, string thingConnectionString)
        {
            using (var sw = new StreamWriter(string.Format(THING_COMMAND_FILENAME, thingId)))
            {
                sw.WriteLine(THING_SETTING_THING_ID, thingId);
                sw.WriteLine(THING_SETTING_COMMAND_TOPIC, thingCommandTopic);
                sw.WriteLine(THING_SETTING_CONNECTION_STRING, thingConnectionString);
                sw.WriteLine(THING_PATH_PARAMETERS, thingPath, thingId, thingCommandTopic, thingConnectionString);
            }

            return Process.Start(thingPath, string.Format(THING_PARAMETERS, thingId, thingCommandTopic, thingConnectionString));
        }

        private static void StopThingProcesses(List<Process> thingProcesses)
        {
            Parallel.ForEach(thingProcesses, 
                             p => 
                             {
                                 try
                                 {
                                     p.Kill();
                                 }
                                 catch
                                 {
                                 }
                                 finally
                                 {
                                     if (p != null)
                                         p.Dispose();
                                 }
                             });
        }

        private static void ProcessCommandLineParameters(string[] args, int partitionAreaLength, ref int activePartition)
        {
            var tempPartition = -1;
            
            if (args.Length > 0)
                tempPartition = int.Parse(args[0]);

            if (tempPartition >= 0 && tempPartition < partitionAreaLength)
                activePartition = tempPartition;
        }
        #endregion
    }
}
