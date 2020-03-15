using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ServiceModel;
using LSHTTP.Gateway.Contract;

namespace LSHTTP.Device
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var runClient = true;

            do
            {
                Console.Clear();

                Console.WriteLine("LSHTTP Device Simulation '{0}'", Process.GetCurrentProcess().Id);
                Console.WriteLine();

                Console.WriteLine("a. Call 'Notification Poll' Service");
                Console.WriteLine("b. Unregister All Devices");

                Console.WriteLine();
                Console.WriteLine("0. Exit Application");
                Console.WriteLine();

                try
                {
                    switch (Console.ReadKey(true).KeyChar)
                    {
                        case 'a':
                            CallNotificationPollService();
                            break;

                        case 'b':
                            UnregisterAllDevices();
                            break;

                        case '0':
                            runClient = false;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\n\nCaught exception: {0}.\n\n", ex);
                }

                if (runClient)
                    Console.ReadKey(true);
            } while (runClient);
        }

        private static void CallNotificationPollService()
        {
            INotification notification;
            var stopWatch = new Stopwatch();
            var notificationFactory = new ChannelFactory<INotification>("LSHTTP.Gateway.Push.Notification");

            try
            {
                Console.WriteLine("Waiting ...");

                stopWatch.Start();

                notificationFactory.Open();
                notification = notificationFactory.CreateChannel();

                notification.BeginPoll("9", "4711",
                                       iar =>
                                       {
                                           var pr = notification.EndPoll(iar);

                                           stopWatch.Stop();

                                           Console.WriteLine("Elapsed Time and Response {0} / {1}",
                                                             stopWatch.Elapsed, !string.IsNullOrWhiteSpace(pr) ? pr : "<empty>");
                                       }, null);
            }
            finally
            {
                if (notificationFactory.State == CommunicationState.Faulted)
                    notificationFactory.Abort();
                else
                    notificationFactory.Close();
            }
        }

        private static void UnregisterAllDevices()
        {
            var notificationFactory = new ChannelFactory<INotification>("LSHTTP.Gateway.Push.Notification");

            try
            {
                Console.WriteLine("Unregistering ...");

                notificationFactory.Open();
                notificationFactory.CreateChannel().UnregisterAll();

                Console.WriteLine("Unregister Completed");
            }
            finally
            {
                if (notificationFactory.State == CommunicationState.Faulted)
                    notificationFactory.Abort();
                else
                    notificationFactory.Close();
            }
        }
    }
}
