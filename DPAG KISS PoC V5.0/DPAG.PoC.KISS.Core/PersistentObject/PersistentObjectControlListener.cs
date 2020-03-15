using System;
using System.Collections.Generic;
using System.Text;
using System.Messaging;

namespace DPAG.PoC.KISS.Core
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class PersistentObjectControlListener : ListenerBase
    {
        private const string FORMAT_PO_CONTROL_POSTFIX = "po-control";

        private int _msmqCapacity;
        private string _sql;

        public PersistentObjectControlListener(string sql, int msmqCapacity) :
            base(sql, FORMAT_PO_CONTROL_POSTFIX)
        {
            _sql = sql;
            _msmqCapacity = msmqCapacity;
        }

        protected override void OnProcessMessage(KissTransactionMode mode, MessageQueue queue, Guid conversation)
        {
            Message poControl;
            PersistentObject po;
            MessageQueueTransaction transaction = null;
            var convMessages = LargeMessage.GetMessagesOfConversation(queue, conversation);
            var allPoIds = PersistentObject.RetrieveAll(_sql);
            var timeout = TimeSpan.FromSeconds(Kiss.GetTimeout(KissTimeout.PO_CONTROL_RECEIVE_TIMEOUT));

            if (mode == KissTransactionMode.DTC)
                throw new NotSupportedException("DTC");

            try
            {
                transaction = new MessageQueueTransaction();
                transaction.Begin();

                poControl = queue.ReceiveById(convMessages[0], timeout, transaction);

                foreach (var poId in allPoIds)
                {
                    po = new PersistentObject(_sql, poId, _msmqCapacity);
                    po.Retrieve();
                    po.Respond(poControl.ResponseQueue, transaction, poControl.Id);
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
