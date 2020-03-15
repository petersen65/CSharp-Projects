using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Runtime.Serialization;
using System.Configuration;
using System.Messaging;
using System.Data;
using System.Data.SqlClient;

namespace DPAG.PoC.KISS.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class PersistentObject : DistributionBase
    {
        private const string FORMAT_PO_POSTFIX = "po";
        private const string FORMAT_PO_IDS = "{0}";
        private const string FORMAT_TO_STRING = "PO: {0}\n";
        private const string PROCEDURE_PO_RETRIEVAL = "[po].[RetrievePersistentObject]";
        private const string VIEW_PO_CATALOG_ALL = "SELECT * FROM [po].[PersistentObjectCatalogAll]";

        private int _msmqCapacity;
        private string _sql, 
                       _id;

        public static string[] RetrieveAll(string sql)
        {
            return RetrieveAll(sql, VIEW_PO_CATALOG_ALL, r => string.Format(FORMAT_PO_IDS, r[0]));
        }

        public PersistentObject(string sql, string id, int msmqCapacity) :
            base(sql, 0, msmqCapacity, string.Format(FORMAT_TO_STRING, id))
        {
            _sql = sql;
            _id = id;
            _msmqCapacity = msmqCapacity;
        }

        public PersistentObject(string sql, string id, int msmqCapacity, byte[] poData) :
            base(sql, poData, 0, msmqCapacity, string.Format(FORMAT_TO_STRING, id))
        {
            _sql = sql;
            _id = id;
            _msmqCapacity = msmqCapacity;
        }

        public void Retrieve()
        {
            Retrieve(PROCEDURE_PO_RETRIEVAL, new[] { new SqlParameter("@po_id", _id) }, "po");
        }

        public void Respond(MessageQueue responseQueue, MessageQueueTransaction transaction, string correlationId)
        {
            var largeMessage = new LargeMessage(DataBuffer, null, _msmqCapacity, correlationId, null);
            var ttrqPo = TimeSpan.FromSeconds(Kiss.GetTimeout(KissTimeout.PO_TTRQ));
            var ttbrPo = TimeSpan.FromSeconds(Kiss.GetTimeout(KissTimeout.PO_TTBR));

            largeMessage.EncodeExtension(new[] { _id, DateTime.Now.ToBinary().ToString(), false.ToString() });
            largeMessage.Send(responseQueue, transaction, ttrqPo, ttbrPo);
        }

        protected override void DistributeMSMQ(KissTransactionMode mode, bool remove, 
                                               string[] filter, LargeMessage largeMessage)
        {
            List<MessageQueue> allQueues = null;
            MessageQueueTransaction msmqTransaction = null;
            var ttrqPo = TimeSpan.FromSeconds(Kiss.GetTimeout(KissTimeout.PO_TTRQ));
            var ttbrPo = TimeSpan.FromSeconds(Kiss.GetTimeout(KissTimeout.PO_TTBR));

            try 
	        {
                largeMessage.EncodeExtension(new[] { _id, DateTime.Now.ToBinary().ToString(), remove.ToString() });
                allQueues = new Neighborhood(_sql, FORMAT_PO_POSTFIX).GetAllQueues(filter);

                if (allQueues.Count > 0)
                {
                    if (mode == KissTransactionMode.MSMQ)
                    {
                        msmqTransaction = new MessageQueueTransaction();
                        msmqTransaction.Begin();
                    }

                    foreach (var queue in allQueues)
                        largeMessage.Send(queue, msmqTransaction, ttrqPo, ttbrPo);

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

                if (allQueues != null)
                {
                    foreach (var queue in allQueues)
                        queue.Dispose();
                }
	        }
        }
    }
}
