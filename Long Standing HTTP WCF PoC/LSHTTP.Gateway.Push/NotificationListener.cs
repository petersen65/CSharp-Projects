using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using LSHTTP.Gateway.Contract;

namespace LSHTTP.Gateway.Push
{
    public sealed class NotificationListener : IDisposable
    {
        private const int MAX_EXCEPTIONS = 100;
        private const int TIMEOUT_CANCELLATION = 1000;
        private const double TIMEOUT_REGISTRATION = 1D * 60D;
        private const string FORMAT_KEY_DEVICE = "{0}-{1}";

        private static object _sync = new object();
        private static NotificationListener _listener = null;

        public static NotificationListener Create()
        {
            if (_listener == null)
            {
                lock (_sync)
                {
                    if (_listener == null)
                        _listener = new NotificationListener();
                }
            }

            return _listener;
        }

        private bool _disposed;
        private Task _worker;
        private CancellationTokenSource _cancellationSource;
        private ReaderWriterLockSlim _disposing;
        private ConcurrentDictionary<string, PollCompletionSource> _devices;

        private NotificationListener()
        {
            _devices = new ConcurrentDictionary<string, PollCompletionSource>();
            _cancellationSource = new CancellationTokenSource();
            _disposing = new ReaderWriterLockSlim();

            _worker = Task.Factory.StartNew(BackgroundWorker, _cancellationSource.Token, TaskCreationOptions.LongRunning);
        }

        public void Register(string device, string operation, PollCompletionSource pollCompletionSource)
        {
            try
            {
                _disposing.EnterReadLock();

                if (_disposed || _cancellationSource.IsCancellationRequested)
                    throw new ObjectDisposedException(GetType().FullName);

                _devices.AddOrUpdate(string.Format(FORMAT_KEY_DEVICE, device, operation), pollCompletionSource,
                                     (ekey, epcs) =>
                                     {
                                         epcs.Done();
                                         return pollCompletionSource;
                                     });
            }
            finally
            {
                _disposing.ExitReadLock();
            }
        }

        private void BackgroundWorker(object parameter)
        {
            var cancellation = (CancellationToken)parameter;
            var timeoutRegistration = TimeSpan.FromSeconds(TIMEOUT_REGISTRATION);
            var innerExceptions = new List<Exception>();

            while (!cancellation.WaitHandle.WaitOne(TIMEOUT_CANCELLATION))
            {
                try
                {
                    Parallel.ForEach(_devices,
                                     (kvp) =>
                                     {
                                         PollCompletionSource pcs;

                                         if (DateTime.Now - kvp.Value.Registration >= timeoutRegistration)
                                         {
                                             kvp.Value.Done();
                                             _devices.TryRemove(kvp.Key, out pcs);
                                         }
                                     });
                }
                catch (Exception ex)
                {
                    innerExceptions.Add(ex);

                    if (innerExceptions.Count >= MAX_EXCEPTIONS)
                        innerExceptions.Clear();
                }
            }

            try
            {
                Parallel.ForEach(_devices, (kvp) => { kvp.Value.Done(); });
            }
            catch (Exception ex)
            {
                innerExceptions.Add(ex);
            }
            finally
            {
                _devices.Clear();
            }

            if (innerExceptions.Count > 0)
                throw new AggregateException(GetType().FullName, innerExceptions);
        }

        #region IDisposable implementation
        public void Dispose()
        {
            try
            {
                _disposing.EnterWriteLock();

                if (!_disposed)
                {
                    try
                    {
                        _cancellationSource.Cancel();
                        _worker.Wait();
                    }
                    catch
                    {
                    }
                    finally
                    {
                        _cancellationSource.Dispose();

                        _worker = null;
                        _cancellationSource = null;
                        _devices = null;

                        _disposed = true;
                        _listener = null;
                    }
                }
            }
            finally
            {
                _disposing.ExitWriteLock();
            }
        }
        #endregion
    }
}
