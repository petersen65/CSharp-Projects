using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOT.DataModel
{
    public interface IPartitionRepository : IDisposable
    {
        IQueryable<Partition> AllPartitions { get; }
        IQueryable<Partition> ActivePartitions { get; }
        
        bool PartitionExists(string ns, bool all = true);
        bool PartitionActive(string ns);
        void ActivatePartition(string ns);
        void DeactivatePartition(string ns);
        Partition GetPartitionByArea(string area, bool all = true);
        Partition GetPartitionByNamespace(string ns, bool all = true);
        void InsertPartition(Partition partition, int commandTopics, int commandSubscriptions,
                             string area, string ns,
                             string owner, string ownerSecret, string storageAccount,
                             string acs, string acsSecret);
        int[] GetRelativeIdsByPartition(Partition partition);
    }
}
