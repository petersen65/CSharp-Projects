using ACS.Management;
using Common.ACS.Management;
using IOT.Core.Helper;
using IOT.DataModel;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace IOT.Server.Provisioning
{
    public sealed class InternetOfThings : IDisposable
    {
        public const int PARTITION_DEF_COMMAND_TOPICS = 5;
        public const int PARTITION_DEF_COMMAND_SUBSCRIPTIONS = 10;
        public const int PARTITION_MAX_COMMAND_TOPICS = 50;
        public const int PARTITION_MAX_COMMAND_SUBSCRIPTIONS = 1000;

        private const string SCHEME_SERVICE_BUS = "sb";

        private const int PARTITION_DEF_MAX_DELIVERY_COUNT = 3;
        private const double PARTITION_DEF_MESSAGE_TTL = 24D;
        private const double PARTITION_DEF_DUPLICATE_DETECTION = 15D;
        private const double PARTITION_DEF_LOCK_DURATION = 5D;
        private const double PARTITION_SECURITY_TOKEN_LIFETIME = 24D * 60D;

        private const string PARTITION_INGESTION_TOPIC = "fan-in";
        private const string PARTITION_EVENT_SUBSCRIPTION = "event";
        private const string PARTITION_ANALYTICS_SUBSCRIPTION = "analytics";
        private const string PARTITION_CONTROL_SUBSCRIPTION = "control";
        private const string PARTITION_COMMAND_TOPIC = "fan-out/{0:00}";
        private const string PARTITION_COMMAND_SUBSCRIPTION = "fan-out/{0:00}/Subscriptions/{1}";

        private const string FILTER_EVENT = "sys.ContentType = 'Event'";
        private const string FILTER_ANALYTICS = "sys.ContentType = 'Event' AND EXISTS(user.Analytics)";
        private const string FILTER_CONTROL = "sys.ContentType = 'Control'";
        private const string FILTER_COMMAND = "sys.To = '{0}' OR EXISTS(user.Broadcast)";

        private const int PASSWORD_BIT_LENGTH = 256;
        private const string NAVIGATION_PROPERTY_SERVICE_IDENTITY_KEYS = "ServiceIdentityKeys";
        private const string NAVIGATION_PROPERTY_RULES = "Rules";
        private const string NAVIGATION_PROPERTY_RELYING_PARTY_ADDRESSES = "RelyingPartyAddresses";
        private const string NAVIGATION_PROPERTY_RELYING_PARTY_RULEGROUPS = "RelyingPartyRuleGroups";
        private const string ISSUER_ACCESS_CONTROL_SERVICE = "LOCAL AUTHORITY";
        private const string DEFAULT_RULE_GROUP_SERVICE_BUS = "Default Rule Group for ServiceBus";
        private const string SERVICE_BUS_CLAIM_TYPE = "net.windows.servicebus.action";
        
        private const string PARTITION_INGESTION_RULEGROUP = "Rule Group for IngestionTopic";
        private const string PARTITION_INGESTION_RELYING_PARTY = "IngestionTopic";
        private const string PARTITION_COMMAND_RULEGROUP = "Rule Group for CommandSubscription - {0}";
        private const string PARTITION_COMMAND_RELYING_PARTY = "CommandSubscription - {0}";
        private const string PARTITION_RELYING_PARTY_ADDRESS = "http://{0}.servicebus.windows.net/{1}";
        private const string PARTITION_TOKEN_TYPE = "SWT";

        private const string EXCEPTION_PARTITION = "Error while executing a partition provisioning operation";
        private const string EXCEPTION_THING = "Error while executing a thing provisioning operation";

        private bool _disposed;
        private int _commandTopics, 
                    _commandSubscriptions,
                    _maxDeliveryCount;
        private TimeSpan _messageTtl, 
                         _duplicateDetection,
                         _lockDuration,
                         _tokenLifeTime;
        private IInternetOfThingsRepository _iotRepository;
        private IPartitionRepository _partitionRepository;
        private IThingRepository _thingRepository;

        public InternetOfThings(int commandTopics = PARTITION_DEF_COMMAND_TOPICS, 
                                int commandSubscriptions = PARTITION_DEF_COMMAND_SUBSCRIPTIONS)
        {
            if (commandTopics <= 0 || _commandTopics > PARTITION_MAX_COMMAND_TOPICS)
                throw new ArgumentOutOfRangeException("commandTopics");

            if (commandSubscriptions <= 0 || commandSubscriptions > PARTITION_MAX_COMMAND_SUBSCRIPTIONS)
                throw new ArgumentOutOfRangeException("commandSubscriptions");
            
            _commandTopics = commandTopics;
            _commandSubscriptions = commandSubscriptions;
            
            _maxDeliveryCount = PARTITION_DEF_MAX_DELIVERY_COUNT;
            _messageTtl = TimeSpan.FromHours(PARTITION_DEF_MESSAGE_TTL);
            _duplicateDetection = TimeSpan.FromMinutes(PARTITION_DEF_DUPLICATE_DETECTION);
            _lockDuration = TimeSpan.FromMinutes(PARTITION_DEF_LOCK_DURATION);
            _tokenLifeTime = TimeSpan.FromMinutes(PARTITION_SECURITY_TOKEN_LIFETIME);

            _iotRepository = InternetOfThingsRepositoryFactory.CreateInternetOfThingsRepository();
            _partitionRepository = _iotRepository as IPartitionRepository;
            _thingRepository = _iotRepository as IThingRepository;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _iotRepository.Dispose();
                _disposed = true;
            }
        }

        #region Partition Management
        public PartitionDescription CreatePartition(PartitionDescription pd)
        {
            return CreatePartitionAsync(pd).Result;
        }

        public async Task<PartitionDescription> CreatePartitionAsync(PartitionDescription pd)
        {
            var stage = ProvisioningStage.None;

            AssertInternetOfThings();
            AssertPartitionDescription(pd);

            try
            {
                stage = ProvisioningStage.StorePartitionInDatabase;

                StorePartitionInDatabase(pd.Description, pd.Area, pd.Namespace, pd.EventStore,
                                         pd.Owner, pd.OwnerSecret, pd.StorageAccount,
                                         pd.AccessControl, pd.AccessControlSecret);

                stage = ProvisioningStage.CreatePartitionInServiceBus;
                await CreatePartitionInServiceBus(pd.Namespace);

                stage = ProvisioningStage.CreatePartitionInTableStorage;
                await CreatePartitionInTableStorage(pd.Namespace);

                stage = ProvisioningStage.CreatePartitionInAccessControl;
                CreatePartitionInAccessControl(pd.Namespace);
            }
            catch (Exception ex)
            {
                throw new ProvisioningException(EXCEPTION_PARTITION, ex) { Stage = stage };
            }

            return pd;
        }

        public void ActivatePartition(PartitionDescription pd)
        {
            AssertInternetOfThings();
            AssertPartitionDescription(pd);

            try
            {
                _partitionRepository.ActivatePartition(pd.Namespace);
            }
            catch (Exception ex)
            {
                throw new ProvisioningException(EXCEPTION_PARTITION, ex) { Stage = ProvisioningStage.ActivatePartition };
            }
        }

        private void AssertPartitionDescription(PartitionDescription pd)
        {
            if (pd == null)
                throw new ArgumentNullException("pd");

            pd.Assert();
        }

        private void StorePartitionInDatabase(string description, string area, string ns, string eventStore,
                                              string owner, string ownerSecret, string storageAccount, string acs, string acsSecret)
        {
            var partition = new Partition
            {
                Description = description,
                Active = false,
                EventStore = eventStore,
            };

            if (!_partitionRepository.PartitionExists(ns))
            {
                _partitionRepository.InsertPartition(partition, _commandTopics, _commandSubscriptions,
                                                     area, ns,
                                                     owner, ownerSecret, storageAccount, acs, acsSecret);
            }
        }

        private async Task CreatePartitionInServiceBus(string ns)
        {
            TopicDescription ingestionTopic; 
            SubscriptionDescription eventSubscription,
                                    analyticsSubscription,
                                    controlSubscription;
            var partition = _partitionRepository.GetPartitionByNamespace(ns);
            var nsMgr = GetNamespaceManager(ns, partition.Owner, partition.OwnerSecret);

            if (await nsMgr.TopicExistsAsync(PARTITION_INGESTION_TOPIC))
                ingestionTopic = await nsMgr.GetTopicAsync(PARTITION_INGESTION_TOPIC);
            else
            {
                ingestionTopic = CreateTopicDescription(PARTITION_INGESTION_TOPIC);
                ingestionTopic = await nsMgr.CreateTopicAsync(ingestionTopic);
            }

            if (!await nsMgr.SubscriptionExistsAsync(ingestionTopic.Path, PARTITION_EVENT_SUBSCRIPTION))
            {
                eventSubscription = CreateSubscriptionDescription(ingestionTopic.Path, PARTITION_EVENT_SUBSCRIPTION);
                await nsMgr.CreateSubscriptionAsync(eventSubscription, new SqlFilter(FILTER_EVENT));
            }

            if (!await nsMgr.SubscriptionExistsAsync(ingestionTopic.Path, PARTITION_ANALYTICS_SUBSCRIPTION))
            {
                analyticsSubscription = CreateSubscriptionDescription(ingestionTopic.Path, PARTITION_ANALYTICS_SUBSCRIPTION);
                await nsMgr.CreateSubscriptionAsync(analyticsSubscription, new SqlFilter(FILTER_ANALYTICS));
            }

            if (!await nsMgr.SubscriptionExistsAsync(ingestionTopic.Path, PARTITION_CONTROL_SUBSCRIPTION))
            {
                controlSubscription = CreateSubscriptionDescription(ingestionTopic.Path, PARTITION_CONTROL_SUBSCRIPTION);
                await nsMgr.CreateSubscriptionAsync(controlSubscription, new SqlFilter(FILTER_CONTROL));
            }

            for (var i = 1; i <= _commandTopics; i++)
            {
                if (!await nsMgr.TopicExistsAsync(string.Format(PARTITION_COMMAND_TOPIC, i)))
                    await nsMgr.CreateTopicAsync(CreateTopicDescription(string.Format(PARTITION_COMMAND_TOPIC, i)));
            }
        }

        private async Task CreatePartitionInTableStorage(string ns)
        {
            CloudTable storageWriterTable;
            var partition = _partitionRepository.GetPartitionByNamespace(ns);
            var cloudTableClient = CloudStorageAccount.Parse(partition.StorageAccount).CreateCloudTableClient();

            cloudTableClient.RetryPolicy = new LinearRetry();
            storageWriterTable = cloudTableClient.GetTableReference(partition.EventStore);
            
            await Task.Factory.FromAsync<bool>(storageWriterTable.BeginCreateIfNotExists, storageWriterTable.EndCreateIfNotExists, null);
        }

        private void CreatePartitionInAccessControl(string ns)
        {
            RuleGroup ingestionRuleGroup;
            RelyingParty ingestionRelyingParty;
            var partition = _partitionRepository.GetPartitionByNamespace(ns);
            var acsMgr = GetAccessControlManagementService(ns, partition.AccessControl, partition.AccessControlSecret);
            var defaultRuleGroup = acsMgr.RuleGroups.Where(rg => rg.Name == DEFAULT_RULE_GROUP_SERVICE_BUS).FirstOrDefault();

            if ((ingestionRuleGroup = acsMgr.RuleGroups.Where(rg => rg.Name == PARTITION_INGESTION_RULEGROUP).FirstOrDefault()) == null)
            {
                ingestionRuleGroup = acsMgr.CreateRuleGroup(PARTITION_INGESTION_RULEGROUP);
                acsMgr.SaveChangesBatch();
            }

            if (acsMgr.RelyingParties.Where(rp => rp.Name == PARTITION_INGESTION_RELYING_PARTY).FirstOrDefault() == null)
            {
                ingestionRelyingParty = CreateRelyingParty(PARTITION_INGESTION_RELYING_PARTY, PARTITION_INGESTION_RELYING_PARTY);
                acsMgr.AddToRelyingParties(ingestionRelyingParty);

                acsMgr.AddRelatedObject(ingestionRelyingParty, 
                                        NAVIGATION_PROPERTY_RELYING_PARTY_ADDRESSES,
                                        CreateRelyingPartyAddress(ns, PARTITION_INGESTION_TOPIC));

                acsMgr.AddRelatedObject(ingestionRelyingParty, 
                                        NAVIGATION_PROPERTY_RELYING_PARTY_RULEGROUPS, 
                                        CreateRelyingPartyRuleGroup(ingestionRuleGroup.Id, ingestionRelyingParty.Id));
                                                                
                acsMgr.AddRelatedObject(ingestionRelyingParty, 
                                        NAVIGATION_PROPERTY_RELYING_PARTY_RULEGROUPS, 
                                        CreateRelyingPartyRuleGroup(defaultRuleGroup.Id, ingestionRelyingParty.Id));
                
                acsMgr.SaveChangesBatch();
            }
        }
        #endregion

        #region Thing Management
        public ThingDescription CreateThing(ThingDescription td)
        {
            return CreateThingAsync(td).Result;
        }

        public async Task<ThingDescription> CreateThingAsync(ThingDescription td)
        {
            int commandTopic;
            string secret;
            var stage = ProvisioningStage.None;

            AssertInternetOfThings();
            AssertThingDescription(td);

            try
            {
                stage = ProvisioningStage.StoreThingInDatabase;
                StoreThingInDatabase(td.Id, td.Description, td.Area);

                stage = ProvisioningStage.CreateThingInServiceBus;
                commandTopic = await CreateThingInServiceBus(td.Id);

                stage = ProvisioningStage.CreateThingInServiceIdentity;
                secret = CreateThingInServiceIdentity(td.Id);

                stage = ProvisioningStage.CreateThingInRuleIngestion;
                CreateThingInRule(td.Id, PARTITION_INGESTION_RULEGROUP, ServiceBusOperation.Send);

                stage = ProvisioningStage.CreateThingInAccessControl;
                CreateThingInAccessControl(td.Id);

                stage = ProvisioningStage.CreateThingInRuleCommand;
                CreateThingInRule(td.Id, string.Format(PARTITION_COMMAND_RULEGROUP, td.Id), ServiceBusOperation.Listen);

                stage = ProvisioningStage.CreateThingConnectionString;
                td.SetConnectionString(CreateThingConnectionString(td.Id, secret));
                td.SetCommandTopic(commandTopic);
            }
            catch (Exception ex)
            {
                throw new ProvisioningException(EXCEPTION_THING, ex) { Stage = stage };
            }

            return td;
        }

        public void ActivateThing(ThingDescription td)
        {
            AssertInternetOfThings();
            AssertThingDescription(td);

            try
            {
                _thingRepository.ActivateThing(td.Id);
            }
            catch (Exception ex)
            {
                throw new ProvisioningException(EXCEPTION_THING, ex) { Stage = ProvisioningStage.ActivateThing };
            }
        }

        private void AssertThingDescription(ThingDescription td)
        {
            if (td == null)
                throw new ArgumentNullException("td");

            td.Assert();
        }

        private void StoreThingInDatabase(Guid id, string description, string area)
        {
            var thing = new Thing
            {
                Description = description,
                Active = false
            };

            if (!_thingRepository.ThingExists(id))
                _thingRepository.InsertThing(thing, id, area);
        }

        private string CreateThingConnectionString(Guid id, string secret)
        {
            var thing = _thingRepository.GetThingById(id);
            return ServiceBusConnection.FromIssuer(thing.Partition.Namespace, id.ToString(), secret);
        }

        private async Task<int> CreateThingInServiceBus(Guid id)
        {
            SubscriptionDescription commandSubscription;
            var thing = _thingRepository.GetThingById(id);
            var partition = thing.Partition;
            var commandTopicPath = string.Format(PARTITION_COMMAND_TOPIC, thing.RelativeId);
            var nsMgr = GetNamespaceManager(partition.Namespace, partition.Owner, partition.OwnerSecret);

            if (!await nsMgr.SubscriptionExistsAsync(commandTopicPath, id.ToString()))
            {
                commandSubscription = CreateSubscriptionDescription(commandTopicPath, id.ToString());
                await nsMgr.CreateSubscriptionAsync(commandSubscription, new SqlFilter(string.Format(FILTER_COMMAND, id)));
            }

            return thing.RelativeId;
        }

        private string CreateThingInServiceIdentity(Guid id)
        {
            ServiceIdentity serviceIdentity;
            var key = new byte[PASSWORD_BIT_LENGTH / 8];
            var thing = _thingRepository.GetThingById(id);
            var partition = thing.Partition;
            var acsMgr = GetAccessControlManagementService(partition.Namespace, partition.AccessControl, partition.AccessControlSecret);

            if ((serviceIdentity = acsMgr.ServiceIdentities.Where(si => si.Name == thing.Id.ToString()).FirstOrDefault()) == null)
            {
                using (var randomKeyGenerator = new RNGCryptoServiceProvider())
                {
                    randomKeyGenerator.GetBytes(key);
                    serviceIdentity = CreateServiceIdentity(thing.Id.ToString(), thing.Description);
                    acsMgr.AddToServiceIdentities(serviceIdentity);

                    acsMgr.AddRelatedObject(serviceIdentity,
                                            NAVIGATION_PROPERTY_SERVICE_IDENTITY_KEYS,
                                            CreateServiceIdentityKey(false, thing.Description, serviceIdentity.Id, key));

                    acsMgr.AddRelatedObject(serviceIdentity,
                                            NAVIGATION_PROPERTY_SERVICE_IDENTITY_KEYS,
                                            CreateServiceIdentityKey(true, thing.Description, serviceIdentity.Id, key));

                    acsMgr.SaveChangesBatch();
                }
            }
            else
            {
                key = acsMgr.ServiceIdentityKeys.Where(sik => sik.ServiceIdentityId == serviceIdentity.Id && 
                                                              sik.Type == ServiceKeyType.Symmetric.ToString()).First().Value;
            }
            
            return Convert.ToBase64String(key);
        }

        private void CreateThingInRule(Guid id, string ruleGroupName, ServiceBusOperation operation)
        {
            var thing = _thingRepository.GetThingById(id);
            var partition = thing.Partition;
            var acsMgr = GetAccessControlManagementService(partition.Namespace, partition.AccessControl, partition.AccessControlSecret);
            var issuerAcs = acsMgr.GetIssuerByName(ISSUER_ACCESS_CONTROL_SERVICE);
            var ruleGroup = acsMgr.RuleGroups.Where(rg => rg.Name == ruleGroupName).FirstOrDefault();
            var rule = CreateRule(thing.Description, issuerAcs.Id, thing.Id.ToString(), operation);

            if (acsMgr.Rules.Where(r => r.RuleGroup.Name == ruleGroupName && 
                                        r.InputClaimValue == rule.InputClaimValue && 
                                        r.OutputClaimValue == rule.OutputClaimValue).FirstOrDefault() == null)
            {
                acsMgr.AddRelatedObject(ruleGroup, NAVIGATION_PROPERTY_RULES, rule);
                acsMgr.SaveChangesBatch();
            }
        }

        private void CreateThingInAccessControl(Guid id)
        {
            RuleGroup commandRuleGroup;
            RelyingParty commandRelyingParty;
            var thing = _thingRepository.GetThingById(id);
            var partition = thing.Partition;
            var ruleGroupName = string.Format(PARTITION_COMMAND_RULEGROUP, thing.Id.ToString());
            var relyingPartName = string.Format(PARTITION_COMMAND_RELYING_PARTY, thing.Id.ToString());
            var subscriptionPath = string.Format(PARTITION_COMMAND_SUBSCRIPTION, thing.RelativeId, thing.Id.ToString());
            var acsMgr = GetAccessControlManagementService(partition.Namespace, partition.AccessControl, partition.AccessControlSecret);
            var defaultRuleGroup = acsMgr.RuleGroups.Where(rg => rg.Name == DEFAULT_RULE_GROUP_SERVICE_BUS).FirstOrDefault();

            if ((commandRuleGroup = acsMgr.RuleGroups.Where(rg => rg.Name == ruleGroupName).FirstOrDefault()) == null)
            {
                commandRuleGroup = acsMgr.CreateRuleGroup(ruleGroupName);
                acsMgr.SaveChangesBatch();
            }

            if (acsMgr.RelyingParties.Where(rp => rp.Name == relyingPartName).FirstOrDefault() == null)
            {
                commandRelyingParty = CreateRelyingParty(relyingPartName, relyingPartName);
                acsMgr.AddToRelyingParties(commandRelyingParty);

                acsMgr.AddRelatedObject(commandRelyingParty,
                                        NAVIGATION_PROPERTY_RELYING_PARTY_ADDRESSES,
                                        CreateRelyingPartyAddress(partition.Namespace, subscriptionPath));

                acsMgr.AddRelatedObject(commandRelyingParty,
                                        NAVIGATION_PROPERTY_RELYING_PARTY_RULEGROUPS,
                                        CreateRelyingPartyRuleGroup(commandRuleGroup.Id, commandRelyingParty.Id));

                acsMgr.AddRelatedObject(commandRelyingParty,
                                        NAVIGATION_PROPERTY_RELYING_PARTY_RULEGROUPS,
                                        CreateRelyingPartyRuleGroup(defaultRuleGroup.Id, commandRelyingParty.Id));

                acsMgr.SaveChangesBatch();
            }
        }
        #endregion

        #region Common Helpers
        private void AssertInternetOfThings()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        private NamespaceManager GetNamespaceManager(string ns, string issuer, string secret, 
                                                     string scheme = SCHEME_SERVICE_BUS, string path = null)
        {
            var tokenProvider = TokenProvider.CreateSharedSecretTokenProvider(issuer, secret);
            var namespaceAddress = ServiceBusEnvironment.CreateServiceUri(scheme, ns, path ?? string.Empty);

            return new NamespaceManager(namespaceAddress, tokenProvider);
        }

        private ManagementService GetAccessControlManagementService(string ns, string acs, string acsSecret)
        {
            var acsConfig = new AccessControlConfiguration(ns, acs, acsSecret);
            var mgmtService = new ManagementServiceHelper(acsConfig);

            return mgmtService.CreateManagementServiceClient();
        }

        private TopicDescription CreateTopicDescription(string path)
        {
            return new TopicDescription(path)
            {
                DefaultMessageTimeToLive = _messageTtl,
                DuplicateDetectionHistoryTimeWindow = _duplicateDetection,
                RequiresDuplicateDetection = true
            };
        }

        private SubscriptionDescription CreateSubscriptionDescription(string path, string name)
        {
            return new SubscriptionDescription(path, name)
            {
                DefaultMessageTimeToLive = _messageTtl,
                RequiresSession = false,
                EnableDeadLetteringOnMessageExpiration = true,
                LockDuration = _lockDuration,
                MaxDeliveryCount = _maxDeliveryCount
            };
        }

        private ServiceIdentity CreateServiceIdentity(string name, string description)
        {
            return new ServiceIdentity
            {
                Name = name,
                Description = description
            };
        }

        private ServiceIdentityKey CreateServiceIdentityKey(bool password, string description, long siid, byte[] key)
        {
            return new ServiceIdentityKey
            {
                DisplayName = description,
                ServiceIdentityId = siid,
                Usage = ServiceKeyUsage.Signing.ToString(),
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.MaxValue,
                Type = password ? ServiceKeyType.Password.ToString() : ServiceKeyType.Symmetric.ToString(),
                Value = password ? Encoding.UTF8.GetBytes(Convert.ToBase64String(key)) : key
            };
        }

        private Rule CreateRule(string description, long iid, string inputClaim, ServiceBusOperation outputClaim)
        {
            return new Rule
            {
                Description = description, 
                IssuerId = iid,
                InputClaimType = ClaimTypes.NameIdentifier,
                InputClaimValue = inputClaim,
                OutputClaimType = SERVICE_BUS_CLAIM_TYPE,
                OutputClaimValue = Enum.GetName(typeof(ServiceBusOperation), outputClaim)
            };
        }

        private RelyingParty CreateRelyingParty(string name, string displayName)
        {
            return new RelyingParty
            {
                Name = name,
                DisplayName = displayName,
                TokenType = PARTITION_TOKEN_TYPE,
                TokenLifetime = Convert.ToInt32(_tokenLifeTime.TotalSeconds),
                AsymmetricTokenEncryptionRequired = false
            };
        }

        private RelyingPartyAddress CreateRelyingPartyAddress(string ns, string path)
        {
            return new RelyingPartyAddress
            {
                Address = string.Format(PARTITION_RELYING_PARTY_ADDRESS, ns, path),
                EndpointType = RelyingPartyAddressType.Realm.ToString()
            };
        }

        private RelyingPartyRuleGroup CreateRelyingPartyRuleGroup(long ruleGroupId, long relyingPartyId)
        {
            return new RelyingPartyRuleGroup 
            {
                RuleGroupId = ruleGroupId,
                RelyingPartyId = relyingPartyId 
            };
        }
        #endregion
    }
}
