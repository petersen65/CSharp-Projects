using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace IOT.Core.Messaging
{
    [DataContract]
    public class EventMessage
    {
        public const string CONTENT_TYPE = "Event";

        [DataMember] public string Type { get; set; }
        [DataMember] public byte[] Body { get; set; }
    }
}
