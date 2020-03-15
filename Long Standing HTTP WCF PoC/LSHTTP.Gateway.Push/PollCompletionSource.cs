using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using LSHTTP.Gateway.Contract;

namespace LSHTTP.Gateway.Push
{
    public sealed class PollCompletionSource : TaskCompletionSource<string>
    {
        private const string DEFAULT_RESULT = "";

        private object _sync;
        private bool _done;
        private AsyncCallback _callback;

        public string Device { get; private set; }
        public string Operation { get; private set; }
        public DateTime Registration { get; private set; }

        public PollCompletionSource(string device, string operation, AsyncCallback callback, object state) : base(state)
        {
            _callback = callback;
            _sync = new object();
            
            Device = device;
            Operation = operation;
            Registration = DateTime.Now;
        }

        public void Done(string result = DEFAULT_RESULT, Exception exception = null)
        {
            lock (_sync)
            {
                if (!_done)
                {
                    try
                    {
                        if (exception == null)
                            SetResult(result);
                        else
                            SetException(exception);

                        _callback(Task);
                    }
                    finally
                    {
                        _done = true;
                    }
                }
            }
        }
    }
}
