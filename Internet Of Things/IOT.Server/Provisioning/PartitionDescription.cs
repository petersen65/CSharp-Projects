using IOT.Core.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOT.Server.Provisioning
{
    public class PartitionDescription
    {
        public string Description { get; private set; }
        public string Area { get; private set; }
        public string Namespace { get; private set; }
        public string EventStore { get; private set; }
        public string Owner { get; set; }
        public string OwnerSecret { get; set; }
        public string StorageAccount { get; set; }
        public string AccessControl { get; set; }
        public string AccessControlSecret { get; set; }

        public PartitionDescription(string description, string area, string ns, string eventStore)
        {
            Description = description;
            Area = area;
            Namespace = ns;
            EventStore = eventStore;
        }

        public string ConnectionString
        {
            get
            {
                return ServiceBusConnection.FromIssuer(Namespace, Owner, OwnerSecret);
            }
        }

        public override string ToString()
        {
            return Namespace;
        }

        internal void Assert()
        {
            if (string.IsNullOrWhiteSpace(Description))
                throw new ArgumentException("Description");

            if (string.IsNullOrWhiteSpace(Area))
                throw new ArgumentException("Area");

            if (string.IsNullOrWhiteSpace(Namespace))
                throw new ArgumentException("Namespace");

            if (string.IsNullOrWhiteSpace(EventStore))
                throw new ArgumentException("EventStore");

            if (string.IsNullOrWhiteSpace(Owner))
                throw new ArgumentException("Owner");

            if (string.IsNullOrWhiteSpace(OwnerSecret))
                throw new ArgumentException("OwnerSecret");

            if (string.IsNullOrWhiteSpace(StorageAccount))
                throw new ArgumentException("StorageAccount");

            if (string.IsNullOrWhiteSpace(AccessControl))
                throw new ArgumentException("AccessControl");

            if (string.IsNullOrWhiteSpace(AccessControlSecret))
                throw new ArgumentException("AccessControlSecret");
        }
    }
}
