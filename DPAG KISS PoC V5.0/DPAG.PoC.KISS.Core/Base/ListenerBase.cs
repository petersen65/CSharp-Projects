using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Configuration;
using System.Transactions;
using System.Data;
using System.Data.SqlClient;
using System.Messaging;

namespace DPAG.PoC.KISS.Core
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class ListenerBase
    {
        private const int APP_SPECIFIC_STOP = -1;
        private const int SLEEP_PEEK_NEXT_MESSAGE = 1 * 50;

        private bool _started;
        private string _sql,
                       _pathPostfix;
        private KissTransactionMode _mode;
        private ManualResetEvent _stopped;
        private MessageQueue _localQueue;

        protected abstract void OnProcessMessage(KissTransactionMode mode, MessageQueue queue, Guid conversation);

        public ListenerBase(string sql, string pathPostfix)
        {
            _sql = sql;
            _pathPostfix = pathPostfix;

            _started = false;
            _stopped = new ManualResetEvent(true);
        }

        public void Start(KissTransactionMode mode)
        {
            if (!_started)
            {
                _mode = mode;
                
                _localQueue = new Neighborhood(_sql, _pathPostfix).GetLocalQueue();
                _localQueue.PeekCompleted += PeekCompleted;
                _localQueue.BeginPeek();

                _started = true;
            }
        }

        public void Stop()
        {
            if (_started && _localQueue != null)
            {
                _stopped.Reset();
                
                using (var stopMsg = new Message { AppSpecific = APP_SPECIFIC_STOP })
                {
                    _localQueue.Send(stopMsg, MessageQueueTransactionType.Single);
                }
            }
        }

        public void WaitUntilStopped()
        {
            _stopped.WaitOne();
        }

        private void DisposeLocalQueue()
        {
            _localQueue.PeekCompleted -= PeekCompleted;
            _localQueue.Dispose();
            _localQueue = null;
            
            _started = false;
            _stopped.Set();
        }

        private void PeekCompleted(object source, PeekCompletedEventArgs eventArgs)
        {
            var abortQueue = false;

            try
            {
                using (var peek = _localQueue.EndPeek(eventArgs.AsyncResult))
                {
                    if (peek.AppSpecific != APP_SPECIFIC_STOP)
                    {
                        foreach (var conversation in LargeMessage.GetAvailableConversations(_localQueue))
                            OnProcessMessage(_mode, _localQueue, conversation);
                    }
                    else
                    {
                        _localQueue.ReceiveById(peek.Id, MessageQueueTransactionType.Single).Dispose();
                        DisposeLocalQueue();
                    }
                }
            }
            catch (MessageQueueException me)
            {
                if (me.MessageQueueErrorCode != MessageQueueErrorCode.IOTimeout &&
                    me.MessageQueueErrorCode != MessageQueueErrorCode.TransactionEnlist)
                    abortQueue = true;
            }
            catch (SqlException)
            {
                abortQueue = true;
            }
            catch (TransactionAbortedException)
            {
                abortQueue = true;
            }
            catch
            {
                abortQueue = true;
            }

            if (abortQueue)
                DisposeLocalQueue();
            else if (_started)
            {
                Thread.Sleep(SLEEP_PEEK_NEXT_MESSAGE);
                _localQueue.BeginPeek();
            }
        }
    }
}
