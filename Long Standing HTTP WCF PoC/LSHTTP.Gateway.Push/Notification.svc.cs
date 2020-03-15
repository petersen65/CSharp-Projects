using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LSHTTP.Gateway.Contract;

namespace LSHTTP.Gateway.Push
{
    [ServiceBehavior(Name = "Notification", Namespace = "http://www.mp.com/lshttp/gateway/service", ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class Notification : INotification
    {
        #region INotification implementation
        public IAsyncResult BeginPoll(string device, string operation, AsyncCallback callback, object state)
        {
            var pollCompletionSource = new PollCompletionSource(device, operation, callback, state);

            NotificationListener.Create().Register(device, operation, pollCompletionSource);
            return pollCompletionSource.Task;
        }

        public string EndPoll(IAsyncResult asyncResult)
        {
            return (asyncResult as Task<string>).Result;
        }

        public void UnregisterAll()
        {
            NotificationListener.Create().Dispose();
        }
        #endregion
    }
}
