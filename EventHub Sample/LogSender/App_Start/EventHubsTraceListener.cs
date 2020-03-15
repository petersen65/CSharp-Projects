using LogContract;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;

namespace LogSender
{
    public class EventHubsTraceListener : TraceListener
    {
        private readonly EventHubClient _client;
        private readonly string _siteName,
                                _instanceId;

        public EventHubsTraceListener()
        {
            var settings = new MessagingFactorySettings
            {
                TokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider("LogSender", "ppPnRmd7fLCNhn6JTe3TMIwE9eMuroaJGgZ/dDEHykU="),
                TransportType = TransportType.Amqp
            };

            var factory = MessagingFactory.Create(ServiceBusEnvironment.CreateServiceUri("sb", "logs-ns", string.Empty), settings);
            
            _client = factory.CreateEventHubClient("logs");
            _instanceId = "localhost";
            _siteName = "LogSender";
        }

        public override void Write(string message)
        {
            var logEvent = new LogEvent 
            {
                MachineName = Environment.MachineName, 
                InstanceId = _instanceId, 
                SiteName = _siteName, 
                Value = message
            };

            var eventData = new EventData(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(logEvent))) 
            { 
                PartitionKey = string.Format("{0}-{1}", _instanceId, DateTime.UtcNow.Ticks)
            };
            
            _client.Send(eventData);
        }

        public override void WriteLine(string message)
        {
            Write(message + Environment.NewLine);
        }
    }
}