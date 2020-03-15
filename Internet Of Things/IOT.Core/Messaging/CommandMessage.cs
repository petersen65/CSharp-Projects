﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace IOT.Core.Messaging
{
    [DataContract]
    public class CommandMessage
    {
        public const string CONTENT_TYPE = "Command";

        [DataMember] public string Command { get; set; }
        [DataMember] public NameValueCollection Parameters { get; set; }
    }
}