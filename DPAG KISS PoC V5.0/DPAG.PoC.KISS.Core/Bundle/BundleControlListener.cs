using System;
using System.Collections.Generic;
using System.Text;
using System.Messaging;

namespace DPAG.PoC.KISS.Core
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class BundleControlListener : ListenerBase
    {
        private const long ID_NO_VALUE = -1;
        private const char SEPARATOR_BUNDLE_IDS = ',';
        private const string FORMAT_BUNDLE_CONTROL_POSTFIX = "bundle-control";

        private int _msmqCapacity;
        private string _sql;

        public BundleControlListener(string sql, int msmqCapacity) :
            base(sql, FORMAT_BUNDLE_CONTROL_POSTFIX)
        {
            _sql = sql;
            _msmqCapacity = msmqCapacity;
        }

        protected override void OnProcessMessage(KissTransactionMode mode, MessageQueue queue, Guid conversation)
        {
            long id, 
                 maxId = ID_NO_VALUE;
            string clientId, 
                   recoveryClient;
            Message bundleControl;
            MessageQueueTransaction transaction = null;
            var convMessages = LargeMessage.GetMessagesOfConversation(queue, conversation);
            var allBundleIds = Bundle.RetrieveAll(_sql);
            var ttrqBundle = TimeSpan.FromSeconds(Kiss.GetTimeout(KissTimeout.BUNDLE_TTRQ));
            var ttbrBundle = TimeSpan.FromSeconds(Kiss.GetTimeout(KissTimeout.BUNDLE_TTBR));
            var timeout = TimeSpan.FromSeconds(Kiss.GetTimeout(KissTimeout.BUNDLE_CONTROL_RECEIVE_TIMEOUT));

            if (mode == KissTransactionMode.DTC)
                throw new NotSupportedException("DTC");

            try
            {
                transaction = new MessageQueueTransaction();
                transaction.Begin();

                bundleControl = queue.ReceiveById(convMessages[0], timeout, transaction);
                recoveryClient = (string)bundleControl.Body;

                foreach (var bundleId in allBundleIds)
                {
                    id = long.Parse(bundleId.Split(SEPARATOR_BUNDLE_IDS)[0]);
                    clientId = bundleId.Split(SEPARATOR_BUNDLE_IDS)[1];

                    if (clientId == recoveryClient && id > maxId)
                        maxId = id;
                }

                using (var msg = new Message(maxId) { CorrelationId = bundleControl.Id,
                                                      Formatter = bundleControl.Formatter,
                                                      TimeToReachQueue = ttrqBundle,
                                                      TimeToBeReceived = ttbrBundle,
                                                      Extension = LargeMessage.ToMessageExtension(Guid.NewGuid()) })
                {
                    bundleControl.ResponseQueue.Send(msg, transaction);
                }

                transaction.Commit();
            }
            finally
            {
                if (transaction != null)
                    transaction.Dispose();
            }
        }
    }
}
