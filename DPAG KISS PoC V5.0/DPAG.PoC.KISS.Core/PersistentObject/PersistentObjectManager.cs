using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Messaging;

namespace DPAG.PoC.KISS.Core
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class PersistentObjectManager
    {
        private const int MAX_RETRY_PO_RECOVERY = 24;
        private const int SLEEP_PO_RECOVERY = 5 * 1000;
        private const string FORMAT_PO_POSTFIX = "po";
        private const string FORMAT_PO_CONTROL_POSTFIX = "po-control";
        private const string FORMAT_PO_RECOVERY_POSTFIX = "po-recovery";
        
        private int _msmqCapacity;
        private string _sql;

        public PersistentObjectManager(string sql, int msmqCapacity)
        {
            _sql = sql;
            _msmqCapacity = msmqCapacity;
        }

        public void InstallMSMQ()
        {
            new Neighborhood(_sql, FORMAT_PO_POSTFIX).CreateLocalQueue(true);
            new Neighborhood(_sql, FORMAT_PO_CONTROL_POSTFIX).CreateLocalQueue(true);
            new Neighborhood(_sql, FORMAT_PO_RECOVERY_POSTFIX).CreateLocalQueue(true);
        }

        public void DeinstallMSMQ()
        {
            new Neighborhood(_sql, FORMAT_PO_POSTFIX).DeleteLocalQueue();
            new Neighborhood(_sql, FORMAT_PO_CONTROL_POSTFIX).DeleteLocalQueue();
            new Neighborhood(_sql, FORMAT_PO_RECOVERY_POSTFIX).DeleteLocalQueue();
        }

        public void RecoverPersistentObjects()
        {
            bool localRecoveryDone;
            List<MessageQueue> controlQueues = null;
            MessageQueue recoveryQueue = null;
            var poListener = new PersistentObjectListener(_sql, _msmqCapacity);
            var ttrqPoControl = TimeSpan.FromSeconds(Kiss.GetTimeout(KissTimeout.PO_CONTROL_TTRQ));
            var ttbrPoControl = TimeSpan.FromSeconds(Kiss.GetTimeout(KissTimeout.PO_CONTROL_TTBR));

            try
            {
                controlQueues = new Neighborhood(_sql, FORMAT_PO_CONTROL_POSTFIX).GetOthersQueues();
                recoveryQueue = new Neighborhood(_sql, FORMAT_PO_RECOVERY_POSTFIX).GetLocalQueue();

                localRecoveryDone = !(controlQueues.Count > 0);

                if (localRecoveryDone)
                    poListener.Recover(recoveryQueue, string.Empty);

                while (!localRecoveryDone)
                {
                    foreach (var controlQueue in controlQueues)
                    {
                        using (var msg = new Message { ResponseQueue = recoveryQueue,
                                                       Formatter = controlQueue.Formatter,
                                                       TimeToReachQueue = ttrqPoControl,
                                                       TimeToBeReceived = ttbrPoControl,
                                                       Extension = LargeMessage.ToMessageExtension(Guid.NewGuid()) })
                        {
                            controlQueue.Send(msg, MessageQueueTransactionType.Single);

                            for (var i = 0; i < MAX_RETRY_PO_RECOVERY && !localRecoveryDone; i++)
                            {
                                Thread.Sleep(SLEEP_PO_RECOVERY);
                                localRecoveryDone = LargeMessage.IsCorrelationAvailable(recoveryQueue, msg.Id);
                            }

                            if (localRecoveryDone)
                            {
                                poListener.Recover(recoveryQueue, msg.Id);
                                break;
                            }
                        }
                    }
                }
            }
            finally
            {
                if (recoveryQueue != null)
                    recoveryQueue.Dispose();

                if (controlQueues != null)
                {
                    foreach (var queue in controlQueues)
                        queue.Dispose();
                }
            }
        }
    }
}
