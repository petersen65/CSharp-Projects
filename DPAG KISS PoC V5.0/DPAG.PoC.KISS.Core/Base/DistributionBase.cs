using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
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
    public abstract class DistributionBase
    {
        protected delegate string RowProjection(DataRow row);

        private int _bufferIndex, 
                    _msmqCapacity;
        private string _sql, 
                       _outputHeader;
        private LargeMessage _largeMessage;

        public int Length
        {
            get
            {
                return _largeMessage.Length;
            }
        }

        protected byte[] DataBuffer
        {
            get
            {
                return _largeMessage.DataBuffer;
            }
        }

        protected abstract void DistributeMSMQ(KissTransactionMode mode, bool remove, 
                                               string[] filter, LargeMessage largeMessage);

        protected static string[] RetrieveAll(string sql, string catalogView, RowProjection FormatRow)
        {
            string[] result;
            var sqltoDistr = Kiss.GetTimeout(KissTimeout.DISTRIBUTION_SQL_RETRIEVE_ALL);

            using (var scope = new TransactionScope(TransactionScopeOption.Suppress))
            using (var kiss = new SqlConnection(Kiss.GetConnection(sql)))
            using (var allIds = new SqlCommand(catalogView, kiss) { CommandTimeout = sqltoDistr, 
                                                                    CommandType = CommandType.Text })
            {
                kiss.Open();

                using (var idsTable = new DataTable())
                using (var idsAdapter = new SqlDataAdapter(allIds))
                {
                    idsAdapter.Fill(idsTable);
                    result = new string[idsTable.Rows.Count];

                    for (var i = 0; i < idsTable.Rows.Count; i++)
                        result[i] = FormatRow(idsTable.Rows[i]);
                }

                kiss.Close();
                scope.Complete();
            }

            return result;
        }

        public static object Retrieve(string sql, string retrieveProcedure, 
                                      SqlParameter[] retrieveParameters, string dataName)
        {
            object retrieveResult = null;
            var sqltoDistr = Kiss.GetTimeout(KissTimeout.DISTRIBUTION_SQL_PROCESS);

            using (var scope = new TransactionScope(TransactionScopeOption.Suppress))
            using (var kiss = new SqlConnection(Kiss.GetConnection(sql)))
            using (var retrieve =
                       new SqlCommand(retrieveProcedure, kiss) { CommandTimeout = sqltoDistr, 
                                                                 CommandType = CommandType.StoredProcedure })
            {
                retrieve.Parameters.AddRange(retrieveParameters);

                kiss.Open();

                using (var dataReader = retrieve.ExecuteReader(CommandBehavior.SingleRow))
                {
                    if (dataReader.HasRows)
                    {
                        dataReader.Read();
                        retrieveResult = dataReader[dataName];
                    }
                }

                kiss.Close();
                scope.Complete();
            }

            return retrieveResult;
        }

        protected DistributionBase(string sql, byte[] dataBuffer, int bufferIndex, 
                                   int msmqCapacity, string outputHeader)
        {
            byte[] dataBufferCopy;

            _sql = sql;
            _msmqCapacity = msmqCapacity;
            _outputHeader = outputHeader;
            _bufferIndex = Math.Abs(bufferIndex);

            if (dataBuffer != null)
            {
                dataBufferCopy = new byte[_bufferIndex + dataBuffer.Length];
                Array.Copy(dataBuffer, 0, dataBufferCopy, _bufferIndex, dataBuffer.Length);
            }
            else
                dataBufferCopy = new byte[_bufferIndex];

            _largeMessage = new LargeMessage(dataBufferCopy, null, msmqCapacity, null, outputHeader);
        }

        protected DistributionBase(string sql, int bufferIndex, int msmqCapacity, string outputHeader) :
            this(sql, null, bufferIndex, msmqCapacity, outputHeader)
        {
        }

        public override string ToString()
        {
            return _largeMessage.ToString(_bufferIndex);
        }

        public void Distribute(KissTransactionMode mode)
        {
            DistributeMSMQ(mode, false, null, _largeMessage);
        }

        public void Distribute(KissTransactionMode mode, string[] filter)
        {
            DistributeMSMQ(mode, false, filter, _largeMessage);
        }

        public void Remove(KissTransactionMode mode)
        {
            DistributeMSMQ(mode, true, null, _largeMessage);
        }

        public void Remove(KissTransactionMode mode, string[] filter)
        {
            DistributeMSMQ(mode, true, filter, _largeMessage);
        }

        protected void SetOutputHeader(string outputHeader)
        {
            _largeMessage.OutputHeader = outputHeader;
        }

        protected bool Retrieve(string retrieveProcedure, SqlParameter[] retrieveParameters, string dataName)
        {
            byte[] dataBuffer = (byte[])Retrieve(_sql, retrieveProcedure, retrieveParameters, dataName);
            var retrieved = dataBuffer != null;

            if (retrieved)
            {
                _bufferIndex = Math.Min(_bufferIndex, dataBuffer.Length);
                _largeMessage = new LargeMessage(dataBuffer, null, _msmqCapacity, null, _outputHeader);
            }

            return retrieved;
        }

        protected void Process(string processProcedure, SqlParameter[] processParameters, 
                               string dataName, out object scalarResult)
        {
            var sqltoDistr = Kiss.GetTimeout(KissTimeout.DISTRIBUTION_SQL_PROCESS);

            using (var kiss = new SqlConnection(Kiss.GetConnection(_sql)))
            using (var process =
                       new SqlCommand(processProcedure, kiss) { CommandTimeout = sqltoDistr, 
                                                                CommandType = CommandType.StoredProcedure })
            {
                process.Parameters.AddRange(processParameters);
                process.Parameters.AddWithValue(dataName, _largeMessage.DataBuffer);

                kiss.Open();
                scalarResult = process.ExecuteScalar();
                kiss.Close();
            }
        }
    }
}
