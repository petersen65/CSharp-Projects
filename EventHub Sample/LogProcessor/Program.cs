using LogContract;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LogProcessor
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var cts = new CancellationTokenSource();

            for (var i = 0; i < 1; i++)
            {
                Task.Factory.StartNew(async partition => 
                {
                    LogEvent logEvent;

                    var settings = new MessagingFactorySettings
                    {
                        TokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider("LogProcessor", "0ihHa1HNpUyhTYELV+FjPbjJhtt0N5PgHoiPaCwuZNs="),
                        TransportType = TransportType.Amqp
                    };

                    var factory = MessagingFactory.Create(ServiceBusEnvironment.CreateServiceUri("sb", "logs-ns", string.Empty), settings);
                    var client = factory.CreateEventHubClient("logs");
                    var group = client.GetDefaultConsumerGroup();
                    var receiver = await group.CreateReceiverAsync(partition.ToString(), DateTime.UtcNow);

                    Console.WriteLine("Worker process started for partition {0} and consumer group {1}", partition, group.GroupName);

                    while (!cts.IsCancellationRequested)
                    {
                        foreach (var eventData in await receiver.ReceiveAsync(10, new TimeSpan(0, 0, 0, 0, 200)))
	                    {
                            logEvent = JsonConvert.DeserializeObject<LogEvent>(Encoding.Unicode.GetString(eventData.GetBytes()));

                            Console.WriteLine("{0} [{1},{2}] {3}: {6}", 
                                              DateTime.Now, 
                                              eventData.PartitionKey, partition,
                                              logEvent.MachineName, logEvent.SiteName, logEvent.InstanceId, logEvent.Value);
	                    }
                    }

                    await receiver.CloseAsync();
                    Console.WriteLine("Worker process finished for partition: {0}", partition);
                }, i as object, TaskCreationOptions.LongRunning);
            }

            Console.ReadKey();
            cts.Cancel();
            Console.ReadKey();
        }
    }
}
