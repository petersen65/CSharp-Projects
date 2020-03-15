using IOT.Core;
using IOT.Core.Helper;
using IOT.Core.Messaging;
using IOT.Core.Policy;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOT.Client
{
    public sealed class ThingsAccess : IDisposable
    {
        private const string PARTITION_INGESTION_TOPIC = "fan-in";
        private const string PARTITION_COMMAND_TOPIC = "fan-out/{0:00}";

        private bool _disposed;
        private Guid _from;
        private int _commandTopic;
        private string _connectionString;
        private TopicClient _topicClient;
        private SubscriptionClient _subscriptionClient;
        private CatchAllPolicy _catchAllPolicy;
        private MessagingPolicy _messagingPolicy;

        public ThingsAccess(Guid from, int commandTopic, string connectionString)
        {
            AssertConstruction(from, commandTopic, connectionString);
            InitializeObjectMembers(from, commandTopic, connectionString);
        }

        public ThingsAccess(Guid from, int commandTopic, string ns, string issuer, string secret)
        {
            AssertConstruction(from, commandTopic, ns, issuer, secret);
            InitializeObjectMembers(from, commandTopic, ServiceBusConnection.FromIssuer(ns, issuer, secret));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                CloseClients();
                _disposed = true;
            }
        }

        #region Event Message Handling
        public void SendEvent(EventMessage eventMessage)
        {
            SendEventAsync(eventMessage).Wait();
        }

        public async Task SendEventAsync(EventMessage eventMessage)
        {
            AssertThingsAccess();

            if (eventMessage == null)
                throw new ArgumentNullException("eventMessage");

            using (var bm = new BrokeredMessage(eventMessage) { ContentType = EventMessage.CONTENT_TYPE })
            {
                bm.ReplyTo = _from.ToString();
                await _messagingPolicy.ExecuteAsync(() => _topicClient.SendAsync(bm));
            }
        }
        #endregion

        #region Control Message Handling
        public void SendControl(ControlMessage controlMessage, Guid? to = null, bool broadcast = true)
        {
            SendControlAsync(controlMessage, to, broadcast).Wait();
        }

        public async Task SendControlAsync(ControlMessage controlMessage, Guid? to = null, bool broadcast = true)
        {
            AssertThingsAccess();

            if (controlMessage == null)
                throw new ArgumentNullException("controlMessage");

            if (broadcast && to != null)
                throw new ArgumentException("broadcast");

            if (!broadcast && to == null)
                throw new ArgumentNullException("to");

            using (var bm = new BrokeredMessage(controlMessage) { ContentType = ControlMessage.CONTENT_TYPE })
            {
                if (broadcast)
                    bm.Properties[MessageProperty.BROADCAST] = 0;
                else
                    bm.To = to.ToString();

                bm.ReplyTo = _from.ToString();
                await _messagingPolicy.ExecuteAsync(() => _topicClient.SendAsync(bm));
            }
        }
        #endregion

        #region Analytics Message Handling
        public void SendAnalytics(AnalyticsMessage analyticsMessage)
        {
            SendAnalyticsAsync(analyticsMessage).Wait();
        }

        public async Task SendAnalyticsAsync(AnalyticsMessage analyticsMessage)
        {
            AssertThingsAccess();

            if (analyticsMessage == null)
                throw new ArgumentNullException("analyticsMessage");

            using (var bm = new BrokeredMessage(analyticsMessage) { ContentType = AnalyticsMessage.CONTENT_TYPE })
            {
                bm.Properties[MessageProperty.ANALYTICS] = 0;

                bm.ReplyTo = _from.ToString();
                await _messagingPolicy.ExecuteAsync(() => _topicClient.SendAsync(bm));
            }
        }
        #endregion

        #region Command Message Handling
        public CommandMessage ReceiveCommand(TimeSpan waitTime)
        {
            return ReceiveCommandAsync(waitTime).Result;
        }

        public async Task<CommandMessage> ReceiveCommandAsync(TimeSpan waitTime)
        {
            BrokeredMessage bm = null;
            CommandMessage commandMessage = null;

            AssertThingsAccess();

            using (bm = await _messagingPolicy.ExecuteAsync(() => _subscriptionClient.ReceiveAsync(waitTime)))
            {
                try
                {
                    if (bm != null)
                    {
                        commandMessage = bm.GetBody<CommandMessage>();
                        await _messagingPolicy.ExecuteAsync(() => bm.CompleteAsync());
                    }
                }
                catch
                {
                    _messagingPolicy.Execute(() => { bm.Abandon(); });
                }
            }

            return commandMessage;
        }
        #endregion

        #region Common Helpers
        private void AssertThingsAccess()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        private void AssertConstruction(Guid from, int commandTopic, string connectionString)
        {
            if (from == Guid.Empty)
                throw new ArgumentException("from");

            if (commandTopic <= 0)
                throw new ArgumentException("commandTopic");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("connectionString");
        }

        private void AssertConstruction(Guid from, int commandTopic, string ns, string issuer, string secret)
        {
            if (from == Guid.Empty)
                throw new ArgumentException("from");

            if (commandTopic <= 0)
                throw new ArgumentException("commandTopic");

            if (string.IsNullOrWhiteSpace(ns))
                throw new ArgumentException("ns");

            if (string.IsNullOrWhiteSpace(issuer))
                throw new ArgumentException("issuer");

            if (string.IsNullOrWhiteSpace(secret))
                throw new ArgumentException("secret");
        }

        private void InitializeObjectMembers(Guid from, int commandTopic, string connectionString)
        {
            _from = from;
            _commandTopic = commandTopic;
            _connectionString = connectionString;

            _catchAllPolicy = new CatchAllPolicy();
            _messagingPolicy = new MessagingPolicy();

            try
            {
                CreateClients();
            }
            catch
            {
                CloseClients();
                throw;
            }
        }
        
        private void CreateClients()
        {
            _topicClient = TopicClient.CreateFromConnectionString(_connectionString,
                                                                    PARTITION_INGESTION_TOPIC);

            _subscriptionClient = SubscriptionClient.CreateFromConnectionString(_connectionString,
                                                                                string.Format(PARTITION_COMMAND_TOPIC, _commandTopic),
                                                                                _from.ToString(),
                                                                                ReceiveMode.PeekLock);
        }

        private void CloseClients()
        {
            if (_subscriptionClient != null)
                _catchAllPolicy.Execute(() => { _subscriptionClient.Close(); });

            if (_topicClient != null)
                _catchAllPolicy.Execute(() => { _topicClient.Close(); });

            _subscriptionClient = null;
            _topicClient = null;
        }
        #endregion
    }
}
