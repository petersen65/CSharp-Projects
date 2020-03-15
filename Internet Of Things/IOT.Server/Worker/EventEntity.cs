using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOT.Server.Worker
{
    internal sealed class EventEntity : TableEntity
    {
        public string Body { get; set; }

        public EventEntity()
        {
        }

        public EventEntity(string partitionKey, string rowKey) : base(partitionKey, rowKey)
        {
        }
    }
}
