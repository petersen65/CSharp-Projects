using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Messaging;
using System.Transactions;

namespace DPAG.PoC.KISS.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class StandingDataListener : ListenerBase
    {
        private const string FORMAT_SD_POSTFIX = "sd";
        private const string PROCEDURE_SD_STORAGE = "[sd].[StoreStandingData]";

        private int _msmqCapacity;
        private string _sql;

        public StandingDataListener(string sql, int msmqCapacity) :
            base(sql, FORMAT_SD_POSTFIX)
        {
            _sql = sql;
            _msmqCapacity = msmqCapacity;
        }

        protected override void OnProcessMessage(KissTransactionMode mode, MessageQueue queue, Guid conversation)
        {
            string[] extension;
            MessageQueueTransaction msmqTransaction = null;
            var convMessages = LargeMessage.GetMessagesOfConversation(queue, conversation);
            var largeMessage = new LargeMessage(_msmqCapacity, null);
            var dtctoSd = TimeSpan.FromSeconds(Kiss.GetTimeout(KissTimeout.SD_DTC_STORAGE));
            var sqltoSd = Kiss.GetTimeout(KissTimeout.SD_SQL_STORAGE);

            try
            {
                using (var scope = new TransactionScope(TransactionScopeOption.Required, dtctoSd))
                using (var kiss = new SqlConnection(Kiss.GetConnection(_sql)))
                using (var sdStorage =
                           new SqlCommand(PROCEDURE_SD_STORAGE, kiss) { CommandTimeout = sqltoSd, 
                                                                        CommandType = CommandType.StoredProcedure })
                {
                    if (mode == KissTransactionMode.DTC)
                    {
                        largeMessage.Receive(queue, null, convMessages,
                                             Kiss.GetTimeout(KissTimeout.SD_RECEIVE_TIMEOUT));
                    }
                    else
                    {
                        using (var suppress = new TransactionScope(TransactionScopeOption.Suppress))
                        {
                            msmqTransaction = new MessageQueueTransaction();
                            msmqTransaction.Begin();

                            largeMessage.Receive(queue, msmqTransaction, convMessages,
                                                 Kiss.GetTimeout(KissTimeout.SD_RECEIVE_TIMEOUT));
                            
                            suppress.Complete();
                        }
                    }

                    extension = largeMessage.DecodeExtension();

                    sdStorage.Parameters.AddWithValue("@sd_id", long.Parse(extension[0]));
                    sdStorage.Parameters.AddWithValue("@blob_sent", DateTime.FromBinary(long.Parse(extension[1])));
                    sdStorage.Parameters.AddWithValue("@blob_received", DateTime.Now);
                    sdStorage.Parameters.AddWithValue("@sd", largeMessage.DataBuffer);

                    kiss.Open();
                    sdStorage.ExecuteNonQuery();
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
