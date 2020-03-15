using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IOT.Core.Policy
{
    public class MessagingPolicy : RetryPolicy<MessagingException>
    {
        public MessagingPolicy() : base(handler: MessagingHandler)
        {
        }

        protected static void MessagingHandler(bool ignore, int delay, int current, int retry, MessagingException ex)
        {
            if (ignore)
                Thread.Sleep(TimeSpan.FromSeconds(Math.Pow(delay, current)));
            else
            {
                if (current == retry + 1)
                    throw ex;
                else
                {
                    if (ex.IsTransient)
                        Thread.Sleep(TimeSpan.FromSeconds(Math.Pow(delay, current)));
                    else
                        throw ex;
                }
            }
        }
    }
}
