using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace IOT.Core.Messaging
{
    [DataContract]
    public class ControlMessage
    {
        public const string CONTENT_TYPE = "Control";

        [DataMember] public string Command { get; set; }
        [DataMember] public NameValueCollection Parameters { get; set; }
    }
}
