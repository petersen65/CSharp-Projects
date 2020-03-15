using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Globalization;
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
    public class Bundle : DistributionBase
    {
        private const int SIZE_BUNDLE_HEADER = 65;
        private const long ID_NO_VALUE = -1;
        private const string FORMAT_BUNDLE_POSTFIX = "bundle";
        private const string FORMAT_BUNDLE_IDS = "{0},{1}";
        private const string FORMAT_TO_STRING = "BUNDLE: {0} {1}\n";
        private const string FORMAT_BUNDLE_SENT = "MM/dd/yyyy HH:mm:ss.fff";
        private const string PROCEDURE_BUNDLE_RETRIEVAL = "[bundle].[RetrieveBundle]";
        private const string PROCEDURE_BUNDLE_PROCESSING = "[bundle].[ProcessBundle]";
        private const string PROCEDURE_BUNDLE_RECOVERY = "[bundle].[StoreBundle]";
        private const string VIEW_BUNDLE_CATALOG_ALL = "SELECT * FROM [bundle].[BundleCatalogAll]";

        private long _id;
        private string _sql,
                       _clientId;

        public static string[] RetrieveAll(string sql)
        {
            return RetrieveAll(sql, VIEW_BUNDLE_CATALOG_ALL, r => string.Format(FORMAT_BUNDLE_IDS, r[0], r[1]));
        }

        public Bundle(string sql, long id, string clientId, int msmqCapacity) :
            base(sql, SIZE_BUNDLE_HEADER, msmqCapacity, string.Format(FORMAT_TO_STRING, id, clientId))
        {
            _sql = sql;
            _id = id;
            _clientId = clientId;
        }

        public Bundle(string sql, string clientId, int msmqCapacity, byte[] bundleData) :
            base(sql, bundleData, SIZE_BUNDLE_HEADER, msmqCapacity, 
                 string.Format(FORMAT_TO_STRING, ID_NO_VALUE, clientId))
        {
            _sql = sql;
            _id = ID_NO_VALUE;
            _clientId = clientId;
        }

        public void Recover()
        {
            var sqltoBundle = Kiss.GetTimeout(KissTimeout.BUNDLE_SQL_RECOVERY);

            using (var kiss = new SqlConnection(Kiss.GetConnection(_sql)))
            using (var bundleRecovery =
                       new SqlCommand(PROCEDURE_BUNDLE_RECOVERY, kiss) { CommandTimeout = sqltoBundle,
                                                                         CommandType = CommandType.StoredProcedure })
            {
                bundleRecovery.Parameters.AddWithValue("@bundle_id", _id);
                bundleRecovery.Parameters.AddWithValue("@client_id", _clientId);

                kiss.Open();
                bundleRecovery.ExecuteNonQuery();
                kiss.Close();
            }
        }

        public void Retrieve()
        {
            Retrieve(PROCEDURE_BUNDLE_RETRIEVAL,
                     new[] { new SqlParameter("@bundle_id", _id), 
                             new SqlParameter("@client_id", _clientId) }, "bundle");
        }

        protected override void DistributeMSMQ(KissTransactionMode mode, bool remove, 
                                               string[] filter, LargeMessage largeMessage)
        {
            object sequenceNumber;
            string bundleSentChar;
            DateTime bundleSent;
            MessageQueueTransaction msmqTransaction = null;
            List<MessageQueue> othersQueues = null;
            var ttrqBundle = TimeSpan.FromSeconds(Kiss.GetTimeout(KissTimeout.BUNDLE_TTRQ));
            var ttbrBundle = TimeSpan.FromSeconds(Kiss.GetTimeout(KissTimeout.BUNDLE_TTBR));
            var dialogLifetime = Kiss.GetTimeout(KissTimeout.BUNDLE_SSSB);
            var dtctoBundle = TimeSpan.FromSeconds(Kiss.GetTimeout(KissTimeout.BUNDLE_DTC_DISTRIBUTION));

            if (remove)
                throw new NotSupportedException("remove");

            try
            {
                othersQueues = new Neighborhood(_sql, FORMAT_BUNDLE_POSTFIX).GetOthersQueues(filter);

                using (var scope = new TransactionScope(TransactionScopeOption.Required, dtctoBundle))
                {
                    bundleSent = DateTime.Now;
                    bundleSentChar = bundleSent.ToString(FORMAT_BUNDLE_SENT, DateTimeFormatInfo.InvariantInfo);

                    Process(PROCEDURE_BUNDLE_PROCESSING,
                            new[] { new SqlParameter("@client_id", _clientId),
                                    new SqlParameter("@bundle_sent", bundleSent), 
                                    new SqlParameter("@bundle_sent_char", bundleSentChar), 
                                    new SqlParameter("@dialog_lifetime", dialogLifetime) }, 
                            "@bundle", out sequenceNumber);

                    largeMessage.EncodeExtension(new[] { sequenceNumber.ToString(), 
                                                         _clientId, bundleSent.ToBinary().ToString() });

                    if (mode == KissTransactionMode.DTC)
                    {
                        foreach (var queue in othersQueues)
                            largeMessage.Send(queue, null, ttrqBundle, ttbrBundle);
                    }
                    else
                    {
                        using (var suppress = new TransactionScope(TransactionScopeOption.Suppress))
                        {
                            msmqTransaction = new MessageQueueTransaction();
                            msmqTransaction.Begin();

                            foreach (var queue in othersQueues)
                                largeMessage.Send(queue, msmqTransaction, ttrqBundle, ttbrBundle);

                            suppress.Complete();
                        }
                    }

                    scope.Complete();
                }

                _id = (long)sequenceNumber;
                SetOutputHeader(string.Format(FORMAT_TO_STRING, _id, _clientId));
                
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

                if (othersQueues != null)
                {
                    foreach (var queue in othersQueues)
                        queue.Dispose();
                }
            }
        }
    }
}
