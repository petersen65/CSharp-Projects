using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IOT.Core.Policy
{
    public class RetryPolicy<E> where E: Exception
    {
        public const int DEFAULT_RETRY = 6;
        public const int DEFAULT_DELAY = 2;

        private int _retry, 
                    _delay;
        private bool _ignore;
        Action<bool, int, int, int, E> _handler;

        public RetryPolicy(Action<bool, int, int, int, E> handler = null, 
                           int retry = DEFAULT_RETRY, int delay = DEFAULT_DELAY, bool ignore = false)
        {
            if (retry <= 0)
                throw new ArgumentOutOfRangeException("retry");

            if (delay <= 1)
                throw new ArgumentOutOfRangeException("delay");

            _retry = retry;
            _delay = delay;
            _ignore = ignore;
            _handler = handler ?? DefaultHandler;
        }

        public async Task ExecuteAsync(Func<Task> func)
        {
            for (var i = 1; i <= _retry + 1 && func != null; i++)
            {
                try
                {
                    await func();
                    break;
                }
                catch (E ex)
                {
                    _handler(_ignore, _delay, i, _retry, ex);
                }
            }
        }

        public async Task<R> ExecuteAsync<R>(Func<Task<R>> func)
        {
            R result = default(R);

            for (var i = 1; i <= _retry + 1 && func != null; i++)
            {
                try
                {
                    result = await func();
                    break;
                }
                catch (E ex)
                {
                    _handler(_ignore, _delay, i, _retry, ex);
                }
            }

            return result;
        }

        public void Execute(Action action)
        {
            for (var i = 1; i <= _retry + 1 && action != null; i++)
            {
                try
                {
                    action();
                    break;
                }
                catch (E ex)
                {
                    _handler(_ignore, _delay, i, _retry, ex);
                }
            }
        }

        protected static void DefaultHandler(bool ignore, int delay, int current, int retry, E ex)
        {
            if (ignore)
                Thread.Sleep(TimeSpan.FromSeconds(Math.Pow(delay, current)));
            else
            {
                if (current == retry + 1)
                    throw ex;
                else
                    Thread.Sleep(TimeSpan.FromSeconds(Math.Pow(delay, current)));
            }
        }
    }
}
