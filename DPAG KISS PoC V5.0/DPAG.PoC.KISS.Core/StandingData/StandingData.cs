using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Messaging;

namespace DPAG.PoC.KISS.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class StandingData : DistributionBase
    {
        private const string FORMAT_SD_POSTFIX = "sd";
        private const string FORMAT_SD_IDS = "{0}";
        private const string FORMAT_TO_STRING = "SD: {0}\n";
        private const string PROCEDURE_SD_RETRIEVAL = "[sd].[RetrieveStandingData]";
        private const string VIEW_SD_CATALOG_ALL = "SELECT * FROM [sd].[StandingDataCatalogAll]";
        private const string VIEW_SD_INSTRUCTION_CATALOG_ALL = "SELECT * FROM [sd].[StandingDataInstructionCatalogAll]";

        private int _msmqCapacity;
        private long _id;
        private string _sql;

        public static string[] RetrieveAll(string sql)
        {
            return RetrieveAll(sql, VIEW_SD_CATALOG_ALL, r => string.Format(FORMAT_SD_IDS, r[0]));
        }

        public static string[] RetrieveInstructionAll(string sql)
        {
            return RetrieveAll(sql, VIEW_SD_INSTRUCTION_CATALOG_ALL, r => string.Format(FORMAT_SD_IDS, r[0]));
        }

        public static string RetrieveInstruction(string sql, long id)
        {
            return (string)Retrieve(sql, PROCEDURE_SD_RETRIEVAL, new[] { new SqlParameter("@sd_id", id) }, "sd_instr");
        }

        public StandingData(string sql, long id, int msmqCapacity) :
            base(sql, 0, msmqCapacity, string.Format(FORMAT_TO_STRING, id))
        {
            _sql = sql;
            _id = id;
            _msmqCapacity = msmqCapacity;
        }

        public StandingData(string sql, long id, int msmqCapacity, byte[] sdData) :
            base(sql, sdData, 0, msmqCapacity, string.Format(FORMAT_TO_STRING, id))
        {
            _sql = sql;
            _id = id;
            _msmqCapacity = msmqCapacity;
        }

        public void Retrieve()
        {
            Retrieve(PROCEDURE_SD_RETRIEVAL, new[] { new SqlParameter("@sd_id", _id) }, "sd");
        }

        protected override void DistributeMSMQ(KissTransactionMode mode, bool remove, 
                                               string[] filter, LargeMessage largeMessage)
        {
            List<MessageQueue> filteredQueues = null;
            MessageQueueTransaction msmqTransaction = null;
            var ttrqSd = TimeSpan.FromSeconds(Kiss.GetTimeout(KissTimeout.SD_TTRQ));
            var ttbrSd = TimeSpan.FromSeconds(Kiss.GetTimeout(KissTimeout.SD_TTBR));

            if (remove)
                throw new NotImplementedException("remove");

            try
            {
                largeMessage.EncodeExtension(new[] { _id.ToString(), DateTime.Now.ToBinary().ToString() });
                filteredQueues = new Neighborhood(_sql, FORMAT_SD_POSTFIX).GetAllQueues(filter);

                if (filteredQueues.Count > 0)
                {
                    if (mode == KissTransactionMode.MSMQ)
                    {
                        msmqTransaction = new MessageQueueTransaction();
                        msmqTransaction.Begin();
                    }

                    foreach (var queue in filteredQueues)
                        largeMessage.Send(queue, msmqTransaction, ttrqSd, ttbrSd);

                    if (mode == KissTransactionMode.MSMQ)
                        msmqTransaction.Commit();
                }
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

                if (filteredQueues != null)
                {
                    foreach (var queue in filteredQueues)
                        queue.Dispose();
                }
            }
        }
    }
}
