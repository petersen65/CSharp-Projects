using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Configuration;
using System.Threading;
using System.Messaging;
using System.Transactions;
using System.Data;
using System.Data.SqlClient;

namespace DPAG.PoC.KISS.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class PersistentObjectListener : ListenerBase
    {
        private const string FORMAT_PO_POSTFIX = "po";
        private const string PROCEDURE_PO_STORAGE = "[po].[StorePersistentObject]";
        private const string PROCEDURE_PO_REMOVAL = "[po].[RemovePersistentObject]";
        private const string PROCEDURE_PO_REMOVAL_ALL = "[po].[RemoveAllPersistentObjects]";

        private int _msmqCapacity;
        private string _sql;

        public PersistentObjectListener(string sql, int msmqCapacity) :
            base(sql, FORMAT_PO_POSTFIX)
        {
            _sql = sql;
            _msmqCapacity = msmqCapacity;
        }

        public void Recover(MessageQueue recoveryQueue, string correlationId)
        {
            var conversations = LargeMessage.GetConversationsOfCorrelation(recoveryQueue, correlationId);
            var dtctoPo = TimeSpan.FromSeconds(Kiss.GetTimeout(KissTimeout.PO_DTC_RECOVERY));
            var sqltoPo = Kiss.GetTimeout(KissTimeout.PO_SQL_RECOVERY);

            using (var scope = new TransactionScope(TransactionScopeOption.Required, dtctoPo))
            using (var kiss = new SqlConnection(Kiss.GetConnection(_sql)))
            using (var poRemoval =
                       new SqlCommand(PROCEDURE_PO_REMOVAL_ALL, kiss) { CommandTimeout = sqltoPo, 
                                                                        CommandType = CommandType.StoredProcedure })
            {
                kiss.Open();
                poRemoval.ExecuteNonQuery();
                kiss.Close();

                foreach (var conversation in conversations)
                    OnProcessMessage(KissTransactionMode.DTC, recoveryQueue, conversation);

                scope.Complete();
            }
        }

        protected override void OnProcessMessage(KissTransactionMode mode, MessageQueue queue, Guid conversation)
        {
            string[] extension;
            MessageQueueTransaction msmqTransaction = null;
            var convMessages = LargeMessage.GetMessagesOfConversation(queue, conversation);
            var largeMessage = new LargeMessage(_msmqCapacity, null);
            var dtctoPo = TimeSpan.FromSeconds(Kiss.GetTimeout(KissTimeout.PO_DTC_STORAGE));
            var sqltoPo = Kiss.GetTimeout(KissTimeout.PO_SQL_STORAGE);

            try
            {
                using (var scope = new TransactionScope(TransactionScopeOption.Required, dtctoPo))
                using (var kiss = new SqlConnection(Kiss.GetConnection(_sql)))
                using (var poStorage =
                           new SqlCommand(PROCEDURE_PO_STORAGE, kiss) { CommandTimeout = sqltoPo, 
                                                                        CommandType = CommandType.StoredProcedure })
                using (var poRemoval =
                           new SqlCommand(PROCEDURE_PO_REMOVAL, kiss) { CommandTimeout = sqltoPo, 
                                                                        CommandType = CommandType.StoredProcedure })
                {
                    if (mode == KissTransactionMode.DTC)
                    {
                        largeMessage.Receive(queue, null, convMessages,
                                             Kiss.GetTimeout(KissTimeout.PO_RECEIVE_TIMEOUT));
                    }
                    else
                    {
                        using (var suppress = new TransactionScope(TransactionScopeOption.Suppress))
                        {
                            msmqTransaction = new MessageQueueTransaction();
                            msmqTransaction.Begin();

                            largeMessage.Receive(queue, msmqTransaction, convMessages,
                                                 Kiss.GetTimeout(KissTimeout.PO_RECEIVE_TIMEOUT));

                            suppress.Complete();
                        }
                    }
                    
                    extension = largeMessage.DecodeExtension();
                    kiss.Open();

                    if (bool.Parse(extension[2]))
                    {
                        poRemoval.Parameters.AddWithValue("@po_id", extension[0]);
                        poRemoval.ExecuteNonQuery();
                    }
                    else
                    {
                        poStorage.Parameters.AddWithValue("@po_id", extension[0]);
                        poStorage.Parameters.AddWithValue("@po_sent", DateTime.FromBinary(long.Parse(extension[1])));
                        poStorage.Parameters.AddWithValue("@po_received", DateTime.Now);
                        poStorage.Parameters.AddWithValue("@po", largeMessage.DataBuffer);

                        poStorage.ExecuteNonQuery();
                    }

                    kiss.Close();
                    scope.Complete();
                }

                if (mode != KissTransactionMode.DTC)
                    msmqTransaction.Commit();
            }
            catch
            {
                if (msmqTransaction != null && mode == KissTransactionMode.MSMQ)
                    msmqTransaction.Abort();

                throw;
            }
            finally
            {
                if (msmqTransaction != null)
                    msmqTransaction.Dispose();
            }
        }
    }
}
