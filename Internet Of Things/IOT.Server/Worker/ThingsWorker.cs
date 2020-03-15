using IOT.Core;
using IOT.Core.Helper;
using IOT.Core.Messaging;
using IOT.Core.Policy;
using IOT.DataModel;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOT.Server.Worker
{
    public sealed class ThingsWorker
    {
        public const int DEF_EVENT_CONCURRENT_CALLS = 1;
        public const int MAX_EVENT_CONCURRENT_CALLS = 10;
        public const int DEF_ANALYTICS_CONCURRENT_CALLS = 1;
        public const int MAX_ANALYTICS_CONCURRENT_CALLS = 10;
        public const int DEF_CONTROL_CONCURRENT_CALLS = 1;
        public const int MAX_CONTROL_CONCURRENT_CALLS = 10;

        private const string PARTITION_INGESTION_TOPIC = "fan-in";
        private const string PARTITION_EVENT_SUBSCRIPTION = "event";
        private const string PARTITION_ANALYTICS_SUBSCRIPTION = "analytics";
        private const string PARTITION_CONTROL_SUBSCRIPTION = "control";
        private const string PARTITION_COMMAND_TOPIC = "fan-out/{0:00}";

        private const string EVENT_ENITY_ROWKEY_DATETIME = "s";

        private bool _disposed;
        private string _cloudStorage, 
                       _connectionString, 
                       _eventStore;
        private SubscriptionClient _eventClient, 
                                   _analyticsClient, 
                                   _controlClient;
        private CatchAllPolicy _catchAllPolicy;
        private MessagingPolicy _messagingPolicy;

        public ThingsWorker(string cloudStorage, string connectionString)
        {
            AssertConstruction(cloudStorage, connectionString);
            InitializeObjectMembers(cloudStorage, connectionString);
        }

        public ThingsWorker(string cloudStorage, string ns, string issuer, string secret)
        {
            AssertConstruction(cloudStorage, ns, issuer, secret);
            InitializeObjectMembers(cloudStorage, ServiceBusConnection.FromIssuer(ns, issuer, secret));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                CloseSubscriptionClients();
                _disposed = true;
            }
        }

        public void RegisterReceiver(int eventConcurrency = DEF_EVENT_CONCURRENT_CALLS, 
                                     int analyticsConcurrency = DEF_ANALYTICS_CONCURRENT_CALLS, 
                                     int controlConcurrency = DEF_CONTROL_CONCURRENT_CALLS)
        {
            OnMessageOptions options;

            AssertThingsWorker();

            if (_eventClient != null || _analyticsClient != null || _controlClient != null)
                throw new InvalidOperationException();

            if (eventConcurrency < 1 || eventConcurrency > MAX_EVENT_CONCURRENT_CALLS)
                throw new ArgumentOutOfRangeException("eventConcurrency");

            if (analyticsConcurrency < 1 || analyticsConcurrency > MAX_ANALYTICS_CONCURRENT_CALLS)
                throw new ArgumentOutOfRangeException("analyticsConcurrency");

            if (controlConcurrency < 1 || controlConcurrency > MAX_CONTROL_CONCURRENT_CALLS)
                throw new ArgumentOutOfRangeException("controlConcurrency");

            try
            {
                CreateSubscriptionClients();

                options = new OnMessageOptions { AutoComplete = true, MaxConcurrentCalls = eventConcurrency };
                options.ExceptionReceived += ExceptionHandler;
                _eventClient.OnMessageAsync(StorageWriter, options);

                options = new OnMessageOptions { AutoComplete = false, MaxConcurrentCalls = analyticsConcurrency };
                options.ExceptionReceived += ExceptionHandler;
                _analyticsClient.OnMessage(Aggregator, options);

                options = new OnMessageOptions { AutoComplete = true, MaxConcurrentCalls = controlConcurrency };
                options.ExceptionReceived += ExceptionHandler;
                _controlClient.OnMessageAsync(ControlSystem, options);
            }
            catch
            {
                CloseSubscriptionClients();
                throw;
            }
        }

        public void UnregisterReceiver()
        {
            AssertThingsWorker();
            CloseSubscriptionClients();
        }

        #region Message Receiver
        private async Task StorageWriter(BrokeredMessage bm)
        {
            await StoreEventEntityInTableStorage(bm, CloudStorageAccount.Parse(_cloudStorage));
        }

        private void Aggregator(BrokeredMessage bm)
        {
        }

        private async Task ControlSystem(BrokeredMessage bm)
        {
            await SendCommandMessageToTargets(bm);
        }

        private void ExceptionHandler(object sender, ExceptionReceivedEventArgs e)
        {
            if (e.Exception != null)
            {
            }
        }
        #endregion

        #region Message Receiver Helper
        private async Task StoreEventEntityInTableStorage(BrokeredMessage bm, CloudStorageAccount cloudStorageAccount)
        {
            CloudTable storageWriterTable;
            EventEntity eventEntity;
            var cloudTableClient = cloudStorageAccount.CreateCloudTableClient();

            cloudTableClient.RetryPolicy = new ExponentialRetry();
            storageWriterTable = cloudTableClient.GetTableReference(_eventStore);
            eventEntity = new EventEntity(bm.ReplyTo, bm.EnqueuedTimeUtc.ToString(EVENT_ENITY_ROWKEY_DATETIME));

            if (bm.Properties.Keys.Contains(MessageProperty.ANALYTICS))
                eventEntity.Body = Encoding.Unicode.GetString(bm.GetBody<AnalyticsMessage>().Body);
            else
                eventEntity.Body = Encoding.Unicode.GetString(bm.GetBody<EventMessage>().Body);

            await Task.Factory.FromAsync<TableOperation, TableResult>(storageWriterTable.BeginExecute,
                                                                      storageWriterTable.EndExecute,
                                                                      TableOperation.Insert(eventEntity), null);
        }

        private async Task SendCommandMessageToTargets(BrokeredMessage bm)
        {
            var to = string.IsNullOrWhiteSpace(bm.To) ? Guid.Empty : new Guid(bm.To);

            using (var iotRepository = InternetOfThingsRepositoryFactory.CreateInternetOfThingsRepository())
            {
                if (bm.Properties.Keys.Contains(MessageProperty.BROADCAST))
                {
                    foreach (var partition in iotRepository.ActivePartitions)
                        await BroadcastCommandMessageToPartition(bm.Clone(), partition, iotRepository.GetRelativeIdsByPartition(partition));
                }
                else if (to != Guid.Empty)
                    await ForwardCommandMessageToThing(bm.Clone(), iotRepository.GetThingById(to, false));
            }
        }

        private async Task BroadcastCommandMessageToPartition(BrokeredMessage bm, Partition partition, int[] relativeIds)
        {
            TopicClient forwardClient = null;
            var connectionString = ServiceBusConnection.FromIssuer(partition.Namespace, partition.Owner, partition.OwnerSecret);
            var controlMessage = bm.GetBody<ControlMessage>();
            var commandMessage = new CommandMessage { Command = controlMessage.Command, Parameters = controlMessage.Parameters };

            foreach (var topicName in relativeIds.Select(id => string.Format(PARTITION_COMMAND_TOPIC, id)))
            {
                try
                {
                    forwardClient = TopicClient.CreateFromConnectionString(connectionString, topicName);

                    using (var forwardMessage = new BrokeredMessage(commandMessage) { ContentType = CommandMessage.CONTENT_TYPE })
                    {
                        forwardMessage.Properties[MessageProperty.BROADCAST] = 0;
                        forwardMessage.ReplyTo = bm.ReplyTo;

                        await _messagingPolicy.ExecuteAsync(() => forwardClient.SendAsync(forwardMessage));
                    }
                }
                finally
                {
                    if (forwardClient != null)
                        _catchAllPolicy.Execute(() => { forwardClient.Close(); });
                }
            }
        }

        private async Task ForwardCommandMessageToThing(BrokeredMessage bm, Thing thing)
        {
            TopicClient forwardClient = null;
            var partition = thing.Partition;
            var topicName = string.Format(PARTITION_COMMAND_TOPIC, thing.RelativeId);
            var connectionString = ServiceBusConnection.FromIssuer(partition.Namespace, partition.Owner, partition.OwnerSecret);
            var controlMessage = bm.GetBody<ControlMessage>();
            var commandMessage = new CommandMessage { Command = controlMessage.Command, Parameters = controlMessage.Parameters };

            try
            {
                forwardClient = TopicClient.CreateFromConnectionString(connectionString, topicName);

                using (var forwardMessage = new BrokeredMessage(commandMessage) { ContentType = CommandMessage.CONTENT_TYPE })
                {
                    forwardMessage.To = bm.To;
                    forwardMessage.ReplyTo = bm.ReplyTo;

                    await _messagingPolicy.ExecuteAsync(() => forwardClient.SendAsync(forwardMessage));
                }
            }
            finally
            {
                if (forwardClient != null)
                    _catchAllPolicy.Execute(() => { forwardClient.Close(); });
            }
        }
        #endregion

        #region Common Helpers
        private void AssertThingsWorker()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        private void AssertConstruction(string cloudStorage, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(cloudStorage))
                throw new ArgumentException("cloudStorage");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("connectionString");
        }

        private void AssertConstruction(string cloudStorage, string ns, string issuer, string secret)
        {
            if (string.IsNullOrWhiteSpace(cloudStorage))
                throw new ArgumentException("cloudStorage");

            if (string.IsNullOrWhiteSpace(ns))
                throw new ArgumentException("ns");

            if (string.IsNullOrWhiteSpace(issuer))
                throw new ArgumentException("issuer");

            if (string.IsNullOrWhiteSpace(secret))
                throw new ArgumentException("secret");
        }

        private void InitializeObjectMembers(string cloudStorage, string connectionString)
        {
            string scheme, 
                   ns;

            _cloudStorage = cloudStorage;
            _connectionString = connectionString;
            
            ServiceBusConnection.ToIssuer(_connectionString, out scheme, out ns);

            using (var partitionRepository = InternetOfThingsRepositoryFactory.CreatePartitionRepository())
            {
                _eventStore = partitionRepository.GetPartitionByNamespace(ns, false).EventStore;
            }

            _catchAllPolicy = new CatchAllPolicy();
            _messagingPolicy = new MessagingPolicy();
        }

        private void CreateSubscriptionClients()
        {
            _eventClient = SubscriptionClient.CreateFromConnectionString(_connectionString,
                                                                         PARTITION_INGESTION_TOPIC,
                                                                         PARTITION_EVENT_SUBSCRIPTION,
                                                                         ReceiveMode.PeekLock);

            _analyticsClient = SubscriptionClient.CreateFromConnectionString(_connectionString,
                                                                             PARTITION_INGESTION_TOPIC,
                                                                             PARTITION_ANALYTICS_SUBSCRIPTION,
                                                                             ReceiveMode.ReceiveAndDelete);

            _controlClient = SubscriptionClient.CreateFromConnectionString(_connectionString,
                                                                           PARTITION_INGESTION_TOPIC,
                                                                           PARTITION_CONTROL_SUBSCRIPTION,
                                                                           ReceiveMode.PeekLock);
        }

        private void CloseSubscriptionClients()
        {
            if (_controlClient != null)
                _catchAllPolicy.Execute(() => { _controlClient.Close(); });

            if (_analyticsClient != null)
                _catchAllPolicy.Execute(() => { _analyticsClient.Close(); });

            if (_eventClient != null)
                _catchAllPolicy.Execute(() => { _eventClient.Close(); });

            _controlClient = _analyticsClient = _eventClient = null;
        }
        #endregion
    }
}
