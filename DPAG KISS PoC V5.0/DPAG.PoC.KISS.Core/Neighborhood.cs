using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Messaging;
using System.Transactions;

namespace DPAG.PoC.KISS.Core
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class Neighborhood
    {
        private const string REPLACE_MSMQ_FORMATNAME = "formatname:direct=os:";
        private const string VIEW_NEIGHBORS_SELF = "SELECT * FROM [kiss].[NeighborsSelf]";
        private const string VIEW_NEIGHBORS_ALL = "SELECT * FROM [kiss].[NeighborsAll]";
        private const string VIEW_NEIGHBORS_OTHERS = "SELECT * FROM [kiss].[NeighborsOthers]";
        
        private string _sql,
                       _pathPostfix;

        public Neighborhood(string sql) : 
            this (sql, string.Empty)
        {
        }

        public Neighborhood(string sql, string pathPostfix)
        {
            _sql = sql;
            _pathPostfix = pathPostfix;
        }

        public bool CreateLocalQueue(bool transactional)
        {
            var queuePath = GetLocalQueuePath();
            var created = !string.IsNullOrEmpty(queuePath) && !MessageQueue.Exists(queuePath);

            if (created)
                MessageQueue.Create(queuePath, transactional).Dispose();

            return created;
        }

        public bool DeleteLocalQueue()
        {
            var queuePath = GetLocalQueuePath();
            var deleted = MessageQueue.Exists(queuePath);

            if (deleted)
                MessageQueue.Delete(queuePath);

            return deleted;
        }

        public MessageQueue GetLocalQueue()
        {
            MessageQueue localQueue = null;
            var neighborhoodQueues = GetNeighborhoodQueues(VIEW_NEIGHBORS_SELF, null);

            for (var i = 1; i < neighborhoodQueues.Count; i++)
                neighborhoodQueues[i].Dispose();

            if (neighborhoodQueues.Count > 0)
            {
                localQueue = neighborhoodQueues[0];

                localQueue.MessageReadPropertyFilter.AppSpecific = true;
                localQueue.MessageReadPropertyFilter.Extension = true;
                localQueue.MessageReadPropertyFilter.IsLastInTransaction = true;
                localQueue.MessageReadPropertyFilter.CorrelationId = true;
            }

            return localQueue;
        }

        public List<MessageQueue> GetAllQueues()
        {
            return GetNeighborhoodQueues(VIEW_NEIGHBORS_ALL, null);
        }

        public List<MessageQueue> GetAllQueues(string[] filter)
        {
            return GetNeighborhoodQueues(VIEW_NEIGHBORS_ALL, filter);
        }

        public List<MessageQueue> GetOthersQueues()
        {
            return GetNeighborhoodQueues(VIEW_NEIGHBORS_OTHERS, null);
        }

        public List<MessageQueue> GetOthersQueues(string[] filter)
        {
            return GetNeighborhoodQueues(VIEW_NEIGHBORS_OTHERS, filter);
        }

        public string GetLocalClient()
        {
            List<string> neighborhoodClients, 
                         neighborhoodBranches;
            string localClient = null;
                
            GetNeighborhoodClients(VIEW_NEIGHBORS_SELF, out neighborhoodClients, out neighborhoodBranches);

            if (neighborhoodClients.Count > 0)
                localClient = neighborhoodClients[0];

            return localClient;
        }

        public string GetLocalBranch()
        {
            List<string> neighborhoodClients,
                         neighborhoodBranches;
            string localBranch = null;

            GetNeighborhoodClients(VIEW_NEIGHBORS_SELF, out neighborhoodClients, out neighborhoodBranches);

            if (neighborhoodBranches.Count > 0)
                localBranch = neighborhoodBranches[0];

            return localBranch;
        }

        public List<string> GetAllClients()
        {
            List<string> neighborhoodClients,
                         neighborhoodBranches;

            GetNeighborhoodClients(VIEW_NEIGHBORS_ALL, out neighborhoodClients, out neighborhoodBranches);
            return neighborhoodClients;
        }

        public List<string> GetOthersClients()
        {
            List<string> neighborhoodClients,
                         neighborhoodBranches;

            GetNeighborhoodClients(VIEW_NEIGHBORS_OTHERS, out neighborhoodClients, out neighborhoodBranches);
            return neighborhoodClients;
        }

        private string GetLocalQueuePath()
        {
            string queuePath = null;

            using (var scope = new TransactionScope(TransactionScopeOption.Suppress))
            using (var kiss = new SqlConnection(Kiss.GetConnection(_sql)))
            using (var self = new SqlCommand(VIEW_NEIGHBORS_SELF, kiss) { CommandType = CommandType.Text })
            {
                kiss.Open();

                using (var clientReader = self.ExecuteReader(CommandBehavior.SingleRow))
                {
                    if (clientReader.HasRows)
                    {
                        clientReader.Read();

                        queuePath = string.Format(((string)clientReader["queue"]).ToLower(),
                                                  ((string)clientReader["dns"]).ToLower(), _pathPostfix);

                        queuePath = queuePath.Replace(REPLACE_MSMQ_FORMATNAME, string.Empty);
                    }
                }

                kiss.Close();
                scope.Complete();
            }

            return queuePath;
        }

        private List<MessageQueue> GetNeighborhoodQueues(string neighborhoodView, string[] filter)
        {
            string queuePath;
            var sortedFilter = filter != null && filter.Length > 0 ? new string[filter.Length] : null;
            var neighborhoodQueues = new List<MessageQueue>();

            if (sortedFilter != null)
            {
                filter.CopyTo(sortedFilter, 0);
                Array.Sort(sortedFilter);
            }

            using (var scope = new TransactionScope(TransactionScopeOption.Suppress))
            using (var kiss = new SqlConnection(Kiss.GetConnection(_sql)))
            using (var neighbors = new SqlCommand(neighborhoodView, kiss) { CommandType = CommandType.Text })
            {
                kiss.Open();

                using (var clientReader = neighbors.ExecuteReader())
                {
                    if (clientReader.HasRows)
                    {
                        while (clientReader.Read())
                        {
                            if ((sortedFilter == null) || 
                                (sortedFilter != null &&
                                 Array.BinarySearch(sortedFilter, (string)clientReader["client_id"]) >= 0))
                            {
                                queuePath = string.Format(((string)clientReader["queue"]).ToLower(),
                                                          ((string)clientReader["dns"]).ToLower(), _pathPostfix);

                                neighborhoodQueues.
                                    Add(new MessageQueue { Path = queuePath, 
                                                           Formatter = new BinaryMessageFormatter() });
                            }
                        }
                    }
                }

                kiss.Close();
                scope.Complete();
            }

            return neighborhoodQueues;
        }

        private void GetNeighborhoodClients(string neighborhoodView,
                                            out List<string> neighborhoodClients, out List<string> neighborhoodBranches)
        {
            neighborhoodClients = new List<string>();
            neighborhoodBranches = new List<string>();

            using (var scope = new TransactionScope(TransactionScopeOption.Suppress))
            using (var kiss = new SqlConnection(Kiss.GetConnection(_sql)))
            using (var neighbors = new SqlCommand(neighborhoodView, kiss) { CommandType = CommandType.Text })
            {
                kiss.Open();

                using (var clientReader = neighbors.ExecuteReader())
                {
                    if (clientReader.HasRows)
                    {
                        while (clientReader.Read())
                        {
                            neighborhoodClients.Add((string)clientReader["client_id"]);
                            neighborhoodBranches.Add((string)clientReader["branch_id"]);
                        }
                    }
                }

                kiss.Close();
                scope.Complete();
            }
        }
    }
}
