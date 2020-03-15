using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Messaging;
using System.Transactions;
using System.Data;
using System.Data.SqlClient;

namespace DPAG.PoC.KISS.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class BundleListener : ListenerBase
    {
        private const string FORMAT_BUNDLE_POSTFIX = "bundle";
        private const string FORMAT_BUNDLE_SENT = "MM/dd/yyyy HH:mm:ss.fff";
        private const string PROCEDURE_BUNDLE_STORAGE = "[bundle].[StoreBundle]";

        private int _msmqCapacity;
        private string _sql;

        public BundleListener(string sql, int msmqCapacity) :
            base(sql, FORMAT_BUNDLE_POSTFIX)
        {
            _sql = sql;
            _msmqCapacity = msmqCapacity;
        }

        protected override void OnProcessMessage(KissTransactionMode mode, MessageQueue queue, Guid conversation)
        {
            string[] extension;
            string bundleSentChar;
            DateTime bundleSent;
            MessageQueueTransaction msmqTransaction = null;
            var convMessages = LargeMessage.GetMessagesOfConversation(queue, conversation);
            var largeMessage = new LargeMessage(_msmqCapacity, null);
            var dtctoBundle = TimeSpan.FromSeconds(Kiss.GetTimeout(KissTimeout.BUNDLE_DTC_STORAGE));
            var sqltoBundle = Kiss.GetTimeout(KissTimeout.BUNDLE_SQL_STORAGE);

            try
            {
                using (var scope = new TransactionScope(TransactionScopeOption.Required, dtctoBundle))
                using (var kiss = new SqlConnection(Kiss.GetConnection(_sql)))
                using (var bundleStorage =
                           new SqlCommand(PROCEDURE_BUNDLE_STORAGE, kiss) { CommandTimeout = sqltoBundle, 
                                                                            CommandType = CommandType.StoredProcedure })
                {
                    if (mode == KissTransactionMode.DTC)
                    {
                        largeMessage.Receive(queue, null, convMessages,
                                             Kiss.GetTimeout(KissTimeout.BUNDLE_RECEIVE_TIMEOUT));
                    }
                    else
                    {
                        using (var suppress = new TransactionScope(TransactionScopeOption.Suppress))
                        {
                            msmqTransaction = new MessageQueueTransaction();
                            msmqTransaction.Begin();

                            largeMessage.Receive(queue, msmqTransaction, convMessages,
                                                 Kiss.GetTimeout(KissTimeout.BUNDLE_RECEIVE_TIMEOUT));

                            suppress.Complete();
                        }
                    }

                    extension = largeMessage.DecodeExtension();

                    bundleSent = DateTime.FromBinary(long.Parse(extension[2]));
                    bundleSentChar = bundleSent.ToString(FORMAT_BUNDLE_SENT, DateTimeFormatInfo.InvariantInfo);

                    bundleStorage.Parameters.AddWithValue("@bundle_id", long.Parse(extension[0]));
                    bundleStorage.Parameters.AddWithValue("@client_id", extension[1]);
                    bundleStorage.Parameters.AddWithValue("@bundle_sent", bundleSent);
                    bundleStorage.Parameters.AddWithValue("@bundle_received", DateTime.Now);
                    bundleStorage.Parameters.AddWithValue("@bundle_sent_char", bundleSentChar);
                    bundleStorage.Parameters.AddWithValue("@bundle", largeMessage.DataBuffer);

                    kiss.Open();
                    bundleStorage.ExecuteNonQuery();
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
