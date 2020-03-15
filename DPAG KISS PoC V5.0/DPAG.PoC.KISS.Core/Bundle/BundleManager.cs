using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Transactions;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Messaging;

namespace DPAG.PoC.KISS.Core
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class BundleManager
    {
        private const int MAX_RETRY_BUNDLE_RECOVERY = 24;
        private const int SLEEP_BUNDLE_RECOVERY = 5 * 1000;
        private const int SLEEP_STAGE_RETRIEVAL = 5 * 1000;
        private const long ID_NOT_RECEIVED_AT_BACKEND = -1;
        private const long ID_NO_ANSWER_FROM_BACKEND = -2;
        private const string FORMAT_BUNDLE_POSTFIX = "bundle";
        private const string FORMAT_BUNDLE_CONTROL_POSTFIX = "bundle-control";
        private const string FORMAT_BUNDLE_RECOVERY_POSTFIX = "bundle-recovery";
        private const string PROCEDURE_BUNDLE_REMOVAL_ALL = "[bundle].[RemoveAllBundles]";
        private const string PROCEDURE_BUNDLE_TRANSMISSION = "[bundle].[TransmitBundles]";
        private const string PROCEDURE_STAGE_LIST_QUERYING = "[bundle].[QueryStageList]";
        private const string PROCEDURE_STAGE_REMOVAL_ALL = "[bundle].[RemoveAllStages]";
        private const string PROCEDURE_STAGE_RETRIEVAL = "[bundle].[RetrieveStage]";
        private const string PROCEDURE_GARBAGE_COLLECTION = "[bundle].[GarbageCollectBundles]";

        private int _msmqCapacity;
        private string _sql,
                       _clientId;

        public BundleManager(string sql, string clientId, int msmqCapacity)
        {
            _sql = sql;
            _clientId = clientId;
            _msmqCapacity = msmqCapacity;
        }

        public void InstallMSMQ()
        {
            new Neighborhood(_sql, FORMAT_BUNDLE_POSTFIX).CreateLocalQueue(true);
            new Neighborhood(_sql, FORMAT_BUNDLE_CONTROL_POSTFIX).CreateLocalQueue(true);
            new Neighborhood(_sql, FORMAT_BUNDLE_RECOVERY_POSTFIX).CreateLocalQueue(true);
        }

        public void DeinstallMSMQ()
        {
            new Neighborhood(_sql, FORMAT_BUNDLE_POSTFIX).DeleteLocalQueue();
            new Neighborhood(_sql, FORMAT_BUNDLE_CONTROL_POSTFIX).DeleteLocalQueue();
            new Neighborhood(_sql, FORMAT_BUNDLE_RECOVERY_POSTFIX).DeleteLocalQueue();
        }

        public void GarbageCollectBundles()
        {
            var sqltoBundle = Kiss.GetTimeout(KissTimeout.KISS_SQL_GARBAGE_COLLECTION);
            
            using (var kiss = new SqlConnection(Kiss.GetConnection(_sql)))
            using (var garbageCollectionExecution = 
                       new SqlCommand(PROCEDURE_GARBAGE_COLLECTION, kiss) { CommandTimeout = sqltoBundle, 
                                                                            CommandType = CommandType.StoredProcedure })
            {
                kiss.Open();
                garbageCollectionExecution.ExecuteNonQuery();
                kiss.Close();
            }
        }

        public bool TransmitBundles()
        {
            bool transmitted;
            long selfId;
            DateTime? selfLastUpdate, 
                      initialLastUpdate;
            var dialogLifetime = Kiss.GetTimeout(KissTimeout.BUNDLE_SSSB);
            var sqltoBundle = Kiss.GetTimeout(KissTimeout.KISS_SQL_STAGELIST);

            WaitForSelfStage(false, null, out selfId, out initialLastUpdate);
            QueryStageList(false);
            transmitted = WaitForSelfStage(true, initialLastUpdate, out selfId, out selfLastUpdate);

            if (transmitted)
            {
                GarbageCollectBundles();

                using (var kiss = new SqlConnection(Kiss.GetConnection(_sql)))
                using (var bundleTransmission =
                           new SqlCommand(PROCEDURE_BUNDLE_TRANSMISSION, kiss) 
                               { CommandTimeout = sqltoBundle, CommandType = CommandType.StoredProcedure })
                {
                    bundleTransmission.Parameters.AddWithValue("@dialog_lifetime", dialogLifetime);

                    kiss.Open();
                    bundleTransmission.ExecuteNonQuery();
                    kiss.Close();
                }
            }

            return transmitted;
        }

        public void RecoverBundles()
        {
            long id, 
                 maxId = ID_NOT_RECEIVED_AT_BACKEND;
            bool responseAvailable,
                 localRecoveryDone, 
                 remoteRecoveryDone;
            DateTime? selfLastUpdate;
            MessageQueue[] controlQueuesOpen, 
                           controlQueues = null;
            MessageQueue recoveryQueue = null;
            var ttrqBundleControl = TimeSpan.FromSeconds(Kiss.GetTimeout(KissTimeout.BUNDLE_CONTROL_TTRQ));
            var ttbrBundleControl = TimeSpan.FromSeconds(Kiss.GetTimeout(KissTimeout.BUNDLE_CONTROL_TTBR));

            try
            {
                controlQueues = new Neighborhood(_sql, FORMAT_BUNDLE_CONTROL_POSTFIX).GetOthersQueues().ToArray();
                controlQueuesOpen = new MessageQueue[controlQueues.Length];
                controlQueues.CopyTo(controlQueuesOpen, 0);
                recoveryQueue = new Neighborhood(_sql, FORMAT_BUNDLE_RECOVERY_POSTFIX).GetLocalQueue();

                localRecoveryDone = !(controlQueues.Length > 0);
                
                while (!localRecoveryDone)
                {
                    for (var i = 0; i < controlQueues.Length; i++)
                    {
                        if (controlQueuesOpen[i] == null)
                            continue;

                        using (var msg = 
                                   new Message(_clientId) { ResponseQueue = recoveryQueue,
                                                            Formatter = controlQueues[i].Formatter,
                                                            TimeToReachQueue = ttrqBundleControl,
                                                            TimeToBeReceived = ttbrBundleControl,
                                                            Extension = 
                                                                LargeMessage.ToMessageExtension(Guid.NewGuid()) })
                        {
                            responseAvailable = false;
                            controlQueues[i].Send(msg, MessageQueueTransactionType.Single);

                            for (var j = 0; j < MAX_RETRY_BUNDLE_RECOVERY && !responseAvailable; j++)
                            {
                                Thread.Sleep(SLEEP_BUNDLE_RECOVERY);
                                responseAvailable = LargeMessage.IsCorrelationAvailable(recoveryQueue, msg.Id);
                            }

                            if (responseAvailable)
                            {
                                using (var response = recoveryQueue.
                                           ReceiveByCorrelationId(msg.Id, MessageQueueTransactionType.Single))
                                {
                                    id = (long)response.Body;
                                    
                                    if (id > maxId)
                                        maxId = id;
                                }

                                controlQueuesOpen[i] = null;
                            }
                        }
                    }

                    localRecoveryDone = Array.TrueForAll(controlQueuesOpen, q => q == null);
                }

                RemoveAllBundlesAndStages();

                do
                {
                    QueryStageList(true);
                    WaitForSelfStage(true, null, out id, out selfLastUpdate);

                    remoteRecoveryDone = id != ID_NO_ANSWER_FROM_BACKEND;

                    if (!remoteRecoveryDone)
                        Thread.Sleep(SLEEP_BUNDLE_RECOVERY);
                } while (!remoteRecoveryDone);

                if (id > maxId)
                    maxId = id;

                if (maxId != ID_NOT_RECEIVED_AT_BACKEND)
                    new Bundle(_sql, maxId, _clientId, _msmqCapacity).Recover();
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

        private void RemoveAllBundlesAndStages()
        {
            var dtctoBundle = TimeSpan.FromSeconds(Kiss.GetTimeout(KissTimeout.BUNDLE_DTC_RECOVERY));
            var sqltoBundle = Kiss.GetTimeout(KissTimeout.BUNDLE_SQL_RECOVERY);

            using (var scope = new TransactionScope(TransactionScopeOption.Required, dtctoBundle))
            using (var kiss = new SqlConnection(Kiss.GetConnection(_sql)))
            using (var bundleRemoval = 
                       new SqlCommand(PROCEDURE_BUNDLE_REMOVAL_ALL, kiss) { CommandTimeout = sqltoBundle,
                                                                            CommandType = CommandType.StoredProcedure })
            using (var stageRemoval =
                       new SqlCommand(PROCEDURE_STAGE_REMOVAL_ALL, kiss) { CommandTimeout = sqltoBundle,
                                                                           CommandType = CommandType.StoredProcedure })
            {
                kiss.Open();
                bundleRemoval.ExecuteNonQuery();
                stageRemoval.ExecuteNonQuery();
                kiss.Close();

                scope.Complete();
            }
        }

        private void QueryStageList(bool self)
        {
            var dialogLifetime = Kiss.GetTimeout(KissTimeout.KISS_SSSB_STAGELIST);
            var sqltoBundle = Kiss.GetTimeout(KissTimeout.KISS_SQL_STAGELIST);

            using (var kiss = new SqlConnection(Kiss.GetConnection(_sql)))
            using (var stageListQuerying = 
                       new SqlCommand(PROCEDURE_STAGE_LIST_QUERYING, kiss) 
                           { CommandTimeout = sqltoBundle, CommandType = CommandType.StoredProcedure })
            {
                stageListQuerying.Parameters.AddWithValue("@self", self ? 1 : 0);
                stageListQuerying.Parameters.AddWithValue("@dialog_lifetime", dialogLifetime);

                kiss.Open();
                stageListQuerying.ExecuteNonQuery();
                kiss.Close();
            }
        }

        private bool WaitForSelfStage(bool wait, DateTime? initialLastUpdate, 
                                      out long selfId, out DateTime? selfLastUpdate)
        {
            bool done;
            SqlParameter sequenceNumber,
                         lastUpdate;
            var dialogLifetime = Kiss.GetTimeout(KissTimeout.KISS_SSSB_STAGELIST);
            var sqltoBundle = Kiss.GetTimeout(KissTimeout.KISS_SQL_STAGELIST);

            selfId = ID_NO_ANSWER_FROM_BACKEND;
            selfLastUpdate = null;

            do
            {
                using (var kiss = new SqlConnection(Kiss.GetConnection(_sql)))
                using (var stageRetrieval =
                           new SqlCommand(PROCEDURE_STAGE_RETRIEVAL, kiss) 
                               { CommandTimeout = sqltoBundle, CommandType = CommandType.StoredProcedure })
                {
                    stageRetrieval.Parameters.AddWithValue("@self", 1);
                    sequenceNumber = stageRetrieval.Parameters.Add("@sequence_number", SqlDbType.BigInt);
                    lastUpdate = stageRetrieval.Parameters.Add("@last_update", SqlDbType.DateTime);
                    
                    sequenceNumber.Direction = lastUpdate.Direction = ParameterDirection.Output;
                    lastUpdate.IsNullable = true;

                    kiss.Open();
                    stageRetrieval.ExecuteNonQuery();
                    kiss.Close();

                    selfId = (long)sequenceNumber.Value;
                    selfLastUpdate = ((SqlDateTime)lastUpdate.SqlValue).IsNull ? null : (DateTime?)lastUpdate.Value;

                    if (wait)
                    {
                        if (initialLastUpdate == null)
                            done = selfId != ID_NO_ANSWER_FROM_BACKEND;
                        else
                            done = selfLastUpdate != initialLastUpdate;
                    }
                    else
                        done = selfId != ID_NO_ANSWER_FROM_BACKEND;
                }

                if (wait && !done)
                    Thread.Sleep(SLEEP_STAGE_RETRIEVAL);
                
                dialogLifetime -= SLEEP_STAGE_RETRIEVAL / 1000;
            } while (dialogLifetime >= 0 && wait && !done);

            return done;
        }
    }
}
