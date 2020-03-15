using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOT.DataModel
{
    internal partial class InternetOfThingsContext : IInternetOfThingsRepository
    {
        private const string EXCEPTION_PARTITION_NOT_FOUND = "Partition not found";
        private const string EXCEPTION_PARTITION_OPTIMISTIC_CONCURRENCY = "Partition not updated due to optimistic concurrency issue";

        private const string EXCEPTION_THING_NOT_FOUND = "Thing not found";
        private const string EXCEPTION_THING_OPTIMISTIC_CONCURRENCY = "Thing not updated due to optimistic concurrency issue";
        private const string EXCEPTION_THING_NO_COMMAND_TOPIC = "Partition does not contain Command Topics with free capacity";

        #region Partition Repository Interface
        public IQueryable<Partition> AllPartitions
        {
            get
            {
                return Partitions;
            }
        }

        public IQueryable<Partition> ActivePartitions
        {
            get
            {
                return Partitions.Where(p => p.Active);
            }
        }

        public bool PartitionExists(string ns, bool all = true)
        {
            if (string.IsNullOrWhiteSpace(ns))
                throw new ArgumentException("ns");

            return all ? Partitions.Any(p => p.Namespace == ns) : Partitions.Any(p => p.Namespace == ns && p.Active);
        }

        public bool PartitionActive(string ns)
        {
            if (string.IsNullOrWhiteSpace(ns))
                throw new ArgumentException("ns");

            return GetPartitionByNamespace(ns).Active;
        }

        public void ActivatePartition(string ns)
        {
            if (string.IsNullOrWhiteSpace(ns))
                throw new ArgumentException("ns");

            try
            {
                GetPartitionByNamespace(ns).Active = true;
                SaveChanges();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new PartitionException(EXCEPTION_PARTITION_OPTIMISTIC_CONCURRENCY, ex);
            }
            catch (OptimisticConcurrencyException ex)
            {
                throw new PartitionException(EXCEPTION_PARTITION_OPTIMISTIC_CONCURRENCY, ex);
            }
        }

        public void DeactivatePartition(string ns)
        {
            if (string.IsNullOrWhiteSpace(ns))
                throw new ArgumentException("ns");

            try
            {
                GetPartitionByNamespace(ns).Active = false;
                SaveChanges();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new PartitionException(EXCEPTION_PARTITION_OPTIMISTIC_CONCURRENCY, ex);
            }
            catch (OptimisticConcurrencyException ex)
            {
                throw new PartitionException(EXCEPTION_PARTITION_OPTIMISTIC_CONCURRENCY, ex);
            }
        }

        public Partition GetPartitionByArea(string area, bool all = true)
        {
            Partition partition;

            if (string.IsNullOrWhiteSpace(area))
                throw new ArgumentException("area");

            try
            {
                partition = all ? Partitions.Single(p => p.Area == area) : Partitions.Single(p => p.Area == area && p.Active);
            }
            catch (Exception ex)
            {
                throw new PartitionException(EXCEPTION_PARTITION_NOT_FOUND, ex);
            }

            return partition;
        }

        public Partition GetPartitionByNamespace(string ns, bool all = true)
        {
            Partition partition;

            if (string.IsNullOrWhiteSpace(ns))
                throw new ArgumentException("ns");

            try
            {
                partition = all ? Partitions.Single(p => p.Namespace == ns) : Partitions.Single(p => p.Namespace == ns && p.Active);
            }
            catch (Exception ex)
            {
                throw new PartitionException(EXCEPTION_PARTITION_NOT_FOUND, ex);
            }

            return partition;
        }


        public void InsertPartition(Partition partition, int commandTopics, int commandSubscriptions, 
                                    string area, string ns, 
                                    string owner, string ownerSecret, string storageAccount, 
                                    string acs, string acsSecret)
        {
            if (partition == null)
                throw new ArgumentNullException("partition");

            if (commandTopics <= 0)
                throw new ArgumentOutOfRangeException("commandTopics");

            if (commandSubscriptions <= 0)
                throw new ArgumentOutOfRangeException("commandSubscriptions");

            if (string.IsNullOrWhiteSpace(area))
                throw new ArgumentException("area");

            if (string.IsNullOrWhiteSpace(ns))
                throw new ArgumentException("ns");

            if (string.IsNullOrWhiteSpace(owner))
                throw new ArgumentException("owner");

            if (string.IsNullOrWhiteSpace(ownerSecret))
                throw new ArgumentException("ownerSecret");

            if (string.IsNullOrWhiteSpace(storageAccount))
                throw new ArgumentException("storageAccount");

            if (string.IsNullOrWhiteSpace(acs))
                throw new ArgumentException("acs");

            if (string.IsNullOrWhiteSpace(acsSecret))
                throw new ArgumentException("acsSecret");

            partition.Area = area;
            partition.MaximumCommandTopic = commandTopics;
            partition.MaximumSubscription = commandSubscriptions;
            partition.Namespace = ns;
            partition.Owner = owner;
            partition.OwnerSecret = ownerSecret;
            partition.StorageAccount = storageAccount;
            partition.AccessControl = acs;
            partition.AccessControlSecret = acsSecret;
            Partitions.Add(partition);

            SaveChanges();
            DetachEntity(partition);
        }

        public int[] GetRelativeIdsByPartition(Partition partition)
        {
            if (partition == null)
                throw new ArgumentNullException("partition");

            return partition.CommandTopics.Select(ct => ct.RelativeId).ToArray();
        }
        #endregion

        #region Thing Repository Interface
        public IQueryable<Thing> AllThings
        {
            get
            {
                return Things;
            }
        }

        public IQueryable<Thing> ActiveThings
        {
            get
            {
                return Things.Where(t => t.Active);
            }
        }

        public bool ThingExists(Guid id, bool all = true)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("id");

            return all ? Things.Any(t => t.Id == id) : Things.Any(t => t.Id == id && t.Active);
        }

        public bool ThingActive(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("id");

            return GetThingById(id).Active;
        }

        public void ActivateThing(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("id");

            try
            {
                GetThingById(id).Active = true;
                SaveChanges();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new ThingException(EXCEPTION_THING_OPTIMISTIC_CONCURRENCY, ex);
            }
            catch (OptimisticConcurrencyException ex)
            {
                throw new ThingException(EXCEPTION_THING_OPTIMISTIC_CONCURRENCY, ex);
            }
        }

        public void DeactivateThing(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("id");

            try
            {
                GetThingById(id).Active = false;
                SaveChanges();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new ThingException(EXCEPTION_THING_OPTIMISTIC_CONCURRENCY, ex);
            }
            catch (OptimisticConcurrencyException ex)
            {
                throw new ThingException(EXCEPTION_THING_OPTIMISTIC_CONCURRENCY, ex);
            }
        }

        public Thing GetThingById(Guid id, bool all = true)
        {
            Thing thing;

            if (id == Guid.Empty)
                throw new ArgumentException("id");

            try
            {
                thing = all ? Things.Single(t => t.Id == id) : Things.Single(t => t.Id == id && t.Active);
            }
            catch (Exception ex)
            {
                throw new ThingException(EXCEPTION_THING_NOT_FOUND, ex);
            }

            return thing;
        }

        public void InsertThing(Thing thing, Guid id, string area)
        {
            if (thing == null)
                throw new ArgumentNullException("thing");

            if (id == Guid.Empty)
                throw new ArgumentException("id");

            if (string.IsNullOrWhiteSpace(area))
                throw new ArgumentException("area");

            try
            {
                thing.Id = id;
                thing.Area = area;
                Things.Add(thing);

                SaveChanges();
                DetachEntity(thing);
            }
            catch (DbUpdateException ex)
            {
                if (ex.GetBaseException().GetType() == typeof(SqlException) && 
                    ex.GetBaseException().Message.Contains(EXCEPTION_THING_NO_COMMAND_TOPIC))
                    throw new ThingException(EXCEPTION_THING_NO_COMMAND_TOPIC, ex);
                else
                    throw;
            }

        }
        #endregion

        #region Common Helpers
        private void DetachEntity<T>(T entity) where T: class
        {
            ((IObjectContextAdapter)this).ObjectContext.Detach(entity);
        }
        #endregion
    }
}
