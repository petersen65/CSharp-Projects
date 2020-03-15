using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace LSHTTP.Gateway.Contract
{
    [DataContract(Name = "PollAction", Namespace = "http://www.mp.com/lshttp/gateway/data")]
    public enum PollAction
    {
        Download,
        OnlineCall
    }

    [DataContract(Name = "PollResult", Namespace = "http://www.mp.com/lshttp/gateway/data")]
    public class PollResult
    {
        public PollResult(string operation, PollAction action, string metadata)
        {
            Operation = operation;
            Action = action;
            Metadata = metadata;
        }

        [DataMember(Name = "Operation", IsRequired = true, Order = 1)]
        public string Operation { get; private set; }

        [DataMember(Name = "Action", IsRequired = true, Order = 2)] 
        public PollAction Action { get; private set; }

        [DataMember(Name = "Metadata", IsRequired = true, Order = 3)]
        public string Metadata { get; private set; }
    }

    [ServiceContract(Name = "INotification", Namespace = "http://www.mp.com/lshttp/gateway/contract")]
    public interface INotification
    {
        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginPoll(string device, string operation, AsyncCallback callback, object state);

        string EndPoll(IAsyncResult asyncResult);
        
        [OperationContract]
        void UnregisterAll();
    }
}
